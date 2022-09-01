using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Breadcrumbs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Breadcrumbs.Attributes
{
    public class BreadcrumbActionFilter : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.HttpContext.Request.Path.HasValue && context.HttpContext.Request.Path.Value.Contains("Api"))
            {
                // This is an API request.  
                base.OnActionExecuted(context);
                return;
            }

            var breadcrumbs = this.ConfigureBreadcrumb(context);

            var controller = context.Controller as Controller;
            controller.ViewBag.Breadcrumbs = breadcrumbs;

            base.OnActionExecuted(context);
        }

        private List<Breadcrumb> ConfigureBreadcrumb(ActionExecutedContext context)
        {
            var breadcrumbList = new List<Breadcrumb>();
            var homeControllerName = "Home";

            breadcrumbList.Add(new Breadcrumb
            {
                Text = "Home",
                Action = "Index",
                Controller = homeControllerName, // Change this controller name to match your Home Controller.
                Active = true
            });

            if (context.HttpContext.Request.Path.HasValue)
            {
                var pathSplit = context.HttpContext.Request.Path.Value.Split("/");
                string controllerName = "";
                for (var i = 0; i < pathSplit.Length; i++)
                {
                    // Check if first element is equal to our Index (home) page.
                    if (string.IsNullOrEmpty(pathSplit[i]) || string.Compare(pathSplit[i], homeControllerName, true) == 0)
                    {
                        continue;
                    }

                    // First check if path is a Controller class.
                    controllerName = context.Controller.ToString().Split(".")[0] + ".Controllers." + pathSplit[i] + "Controller";
                    var controller = this.GetControllerType(controllerName);

                    // If this is a controller, does it have a default Index method? If so, that needs adding as a breadcrumb. Is the next path element called Index?
                    if (controller != null)
                    {
                        var indexMethod = controller.GetMethod("Index");

                        if (indexMethod != null)
                        {
                            breadcrumbList.Add(new Breadcrumb
                            {
                                Text = this.CamelCaseSpacing(pathSplit[i]),
                                Action = "Index",
                                Controller = pathSplit[i],
                                Active = true
                            });

                            if (i + 1 < pathSplit.Length && string.Compare(pathSplit[i + 1], "Index", true) == 0)
                            {
                                // This is the last element in the breadcrumb list. This should be disabled.
                                breadcrumbList.LastOrDefault().Active = false;

                                // Next path item is the Index method. We can escape from this method now.
                                return breadcrumbList;
                            }
                        }
                    }

                    // If not a Controller, check if this is a method on the previous path element.
                    if (i - 1 > 0)
                    {
                        controllerName = context.Controller.ToString().Split(".")[0] + ".Controllers." + pathSplit[i - 1] + "Controller";
                        var prevController = this.GetControllerType(controllerName);

                        if (prevController != null)
                        {
                            var existeMetodo = false;
                            try
                            {
                                var method = prevController.GetMethod(pathSplit[i]);
                                existeMetodo = method != null;
                            }
                            catch (AmbiguousMatchException)
                            {
                                existeMetodo = true;
                                Console.WriteLine("{0}.{1} has multiple public overloads.",
                                                  prevController.Name, pathSplit[i]);
                            }

                            if (existeMetodo)
                            {
                                // We've found an endpoint on the previous controller.
                                breadcrumbList.Add(new Breadcrumb
                                {
                                    Text = this.CamelCaseSpacing(pathSplit[i]),
                                    Action = pathSplit[i],
                                    Controller = pathSplit[i - 1]
                                });
                            }

                        }
                    }
                }
            }

            // There will always be at least 1 entry in the breadcrumb list. The last element should always be disabled.
            breadcrumbList.LastOrDefault().Active = false;

            return breadcrumbList;
        }

        private Type GetControllerType(string name)
        {
            Type controller = null;

            try
            {
                controller = Assembly.GetCallingAssembly().GetType(name);
            }
            catch
            { }

            return controller;
        }

        private string CamelCaseSpacing(string s)
        {
            // Sourced from https://stackoverflow.com/questions/4488969/split-a-string-by-capital-letters.
            var r = new Regex(@"
            (?<=[A-Z])(?=[A-Z][a-z]) |
             (?<=[^A-Z])(?=[A-Z]) |
             (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

            return r.Replace(s, " ");
        }

    }
}
