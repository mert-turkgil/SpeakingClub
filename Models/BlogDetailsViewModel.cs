using System;
using System.Collections.Generic;
using SpeakingClub.Entity;  // For Category, Tag, Quiz, and Question

namespace SpeakingClub.Models
{
    // A view model that covers all Blog properties.
    public class BlogDetailViewModel
    {
        // Basic blog properties
        public int BlogId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Author { get; set; } = string.Empty;

        // Additional properties present in your Blog entity
        public string RawYT { get; set; } = string.Empty;
        public string RawMaps { get; set; } = string.Empty;
        public int ViewCount { get; set; }

        // Category related fields
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }  // e.g., Category.Name

        // Navigation properties for related collections
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();

        // The Blog entity defines a collection named "Quiz".

        // Change the types here to use the Entity versions explicitly.
        public ICollection<SpeakingClub.Entity.Quiz> Quizzes { get; set; } = new List<SpeakingClub.Entity.Quiz>();

        // Also use the entity type for Question.
        public SpeakingClub.Entity.Question? QuizQuestion { get; set; }
    }
}
