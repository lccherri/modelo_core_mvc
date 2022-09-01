using Breadcrumbs.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace modelo_core_mvc.Controllers
{
    [BreadcrumbActionFilter]
    public class BaseController : Controller
    {
    }
}
