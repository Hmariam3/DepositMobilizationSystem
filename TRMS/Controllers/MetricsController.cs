using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Web.Mvc;
using Prometheus;

namespace TRMS.Controllers
{
    public class MetricsController : Controller
    {
        public async Task<ActionResult> Index()
        {
            // Set Prometheus content type
            Response.ContentType = "text/plain; version=0.0.4";

            // Collect and write metrics directly to the response stream
            await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(Response.OutputStream);

            return new EmptyResult();
        }
    }
}