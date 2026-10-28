using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace SpeakingClub.Services.Email
{
    /// <summary>
    /// Renders Views/Emails/{template}.cshtml to HTML outside any web request (the e-mails are sent by a background
    /// service), in the recipient's language. The template sets ViewData["Subject"].
    /// </summary>
    public class EmailRenderer
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempData;
        private readonly IServiceProvider _services;

        public EmailRenderer(IRazorViewEngine viewEngine, ITempDataProvider tempData, IServiceProvider services)
        {
            _viewEngine = viewEngine;
            _tempData = tempData;
            _services = services;
        }

        public async Task<RenderedEmail> RenderAsync(string template, object model, string culture)
        {
            var actionContext = new ActionContext(new DefaultHttpContext { RequestServices = _services }, new RouteData(), new ActionDescriptor());
            var path = $"~/Views/Emails/{template}.cshtml";
            var view = _viewEngine.GetView(executingFilePath: null, viewPath: path, isMainPage: true);
            if (!view.Success)
                throw new InvalidOperationException($"E-mail template not found: {path}");

            await using var writer = new StringWriter();
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model };
            var context = new ViewContext(actionContext, view.View, viewData,
                new TempDataDictionary(actionContext.HttpContext, _tempData), writer, new HtmlHelperOptions());

            // No request sets the culture here: it is set for exactly this render and put back afterwards, so one
            // e-mail's language never leaks into the next one the service renders on the same thread
            var resolved = new CultureInfo(UserLanguage.Normalize(culture));
            var (previous, previousUi) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
            try
            {
                CultureInfo.CurrentCulture = resolved;
                CultureInfo.CurrentUICulture = resolved;
                await view.View.RenderAsync(context);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
                CultureInfo.CurrentUICulture = previousUi;
            }
            // Read from the context: the page got its own copy of ViewData
            return new RenderedEmail(writer.ToString(), context.ViewData["Subject"] as string ?? template);
        }
    }
}
