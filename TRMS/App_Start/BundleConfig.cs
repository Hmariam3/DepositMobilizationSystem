using System.Web;
using System.Web.Optimization;

namespace TRMS
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/admin-lte/jsFiles").Include(
            "~/admin-lte/js/adminlte.js",
            "~/admin-lte/plugins/datatables.net/js/jquery.dataTables.min.js",
            "~/admin-lte/plugins/fullcalendar/dist/fullcalendar.min.js",

            "~/admin-lte/plugins/datatables.net-bs/js/dataTables.bootstrap.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at https://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/site.css",
                      "~/admin-lte/css/AdminLTE.css",
                      "~/admin-lte/css/skins/skin-red.css",
                      "~/admin-lte/plugins/datatables.net-bs/css/dataTables.bootstrap.min.css",
                      "~/Content/font-awesome.css",
                      "~/Content/AjaxLoader.css"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryui").Include(
          "~/Scripts/jquery-ui-{version}.js"));


            bundles.Add(new ScriptBundle("~/Content/jquery-3.6.0").Include(
             "~/Content/jquery-3.6.0.min.js"));
            //css  
            bundles.Add(new StyleBundle("~/Content/cssjqryUi").Include(
                   "~/Content/themes/base/jquery-ui.min.css"));
        }
    }
}
