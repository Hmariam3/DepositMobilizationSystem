using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using TRMS.Security;
using TRMS;

using Prometheus;


    public static class AppMetrics
    {


        public static readonly Counter HttpRequests =
            Metrics.CreateCounter(
                "trms_http_requests_total",
                "Total HTTP requests",
                new CounterConfiguration
                {
                    LabelNames = new[] { "method", "endpoint", "status" }
                });

        public static readonly Histogram HttpRequestDuration =
            Metrics.CreateHistogram(
                "trms_http_request_duration_seconds",
                "HTTP request duration",
                new HistogramConfiguration
                {
                    LabelNames = new[] { "endpoint" }
                });



    //    public static readonly Counter RequestCounter =
    //        Metrics.CreateCounter("trms_requests_total", "Total requests");

    //    public static readonly Counter ErrorCounter =
    //        Metrics.CreateCounter("trms_errors_total", "Total errors");

    //    public static readonly Histogram RequestDuration =
    //        Metrics.CreateHistogram(
    //            "trms_request_duration_seconds",
    //            "Request duration in seconds"
    //        );
    }
