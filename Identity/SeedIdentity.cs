using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace SpeakingClub.Identity
{
    public static class SeedIdentity
    {
      public static async Task Seed(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            // Get roles from configuration and ensure each exists
            var roles = configuration.GetSection("Data:Roles").GetChildren()
                .Select(x => x.Value)
                .Where(role => !string.IsNullOrEmpty(role))
                .ToArray();

            foreach (var role in roles)
            {
                if (role == null)
                    continue; // Safety check
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!roleResult.Succeeded)
                    {
                        Console.WriteLine($"Failed to create role {role}: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                    }
                }
            }

            // Get users from configuration
            var users = configuration.GetSection("Data:Users").GetChildren();

            foreach (var section in users)
            {
                var username = section.GetValue<string>("username");
                var password = section.GetValue<string>("password");
                var email = section.GetValue<string>("email");

                // Split roles if multiple roles are provided (e.g., "ADMIN,ROOT")
                var roleString = section.GetValue<string>("role");
                var rolesForUser = roleString?
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim())
                    .ToArray() ?? new string[0];

                var firstName = section.GetValue<string>("firstName");
                var lastName = section.GetValue<string>("lastName");

                // Validate required fields
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(email) || rolesForUser.Length == 0)
                {
                    Console.WriteLine("Skipping invalid user due to missing required fields.");
                    continue;
                }

                // Check if user already exists
                var existingUser = await userManager.FindByNameAsync(username);
                if (existingUser == null)
                {
                    var user = new User()
                    {
                        UserName = username,
                        Email = email,
                        FirstName = firstName ?? "DefaultFirstName",
                        LastName = lastName ?? "DefaultLastName",
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, password ?? throw new ArgumentNullException(nameof(password), "Password cannot be null."));
                    if (result.Succeeded)
                    {
                        foreach (var userRole in rolesForUser)
                        {
                            var addRoleResult = await userManager.AddToRoleAsync(user, userRole);
                            if (!addRoleResult.Succeeded)
                            {
                                Console.WriteLine($"Failed to add role {userRole} to user {username}: {string.Join(", ", addRoleResult.Errors.Select(e => e.Description))}");
                            }
                        }
                        Console.WriteLine($"User '{username}' created successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to create user: {username}. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    Console.WriteLine($"User '{username}' already exists — resetting password and syncing roles.");

                    // Reset password in case it was set to a fake/invalid hash
                    var resetToken = await userManager.GeneratePasswordResetTokenAsync(existingUser);
                    var resetResult = await userManager.ResetPasswordAsync(existingUser, resetToken, password);
                    if (!resetResult.Succeeded)
                    {
                        Console.WriteLine($"Failed to reset password for '{username}': {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");
                    }

                    // Ensure EmailConfirmed so login is not blocked
                    if (!existingUser.EmailConfirmed)
                    {
                        existingUser.EmailConfirmed = true;
                        await userManager.UpdateAsync(existingUser);
                    }

                    // Add any missing roles
                    var currentRoles = await userManager.GetRolesAsync(existingUser);
                    foreach (var userRole in rolesForUser)
                    {
                        if (!currentRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
                        {
                            var addRoleResult = await userManager.AddToRoleAsync(existingUser, userRole);
                            if (!addRoleResult.Succeeded)
                            {
                                Console.WriteLine($"Failed to add role {userRole} to user {username}: {string.Join(", ", addRoleResult.Errors.Select(e => e.Description))}");
                            }
                        }
                    }
                }
            }
        }
    }
}