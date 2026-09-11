/**
 * OpenTelemetry Browser Instrumentation
 *
 * This module initializes OpenTelemetry for browser-based tracing.
 * It sets up:
 * - WebTracerProvider for creating traces
 * - DocumentLoadInstrumentation for tracking page load performance
 * - UserInteractionInstrumentation for tracking user interactions
 * - XMLHttpRequestInstrumentation for tracking HTTP requests
 * - OTLPTraceExporter for sending traces to the OpenTelemetry Collector via HTTP/protobuf
 * - ZoneContextManager for proper async operation tracking
 */

import { OTLPTraceExporter } from "@opentelemetry/exporter-trace-otlp-proto";
import { BatchSpanProcessor } from "@opentelemetry/sdk-trace-base";
import { WebTracerProvider } from "@opentelemetry/sdk-trace-web";
import { DocumentLoadInstrumentation } from "@opentelemetry/instrumentation-document-load";
import { UserInteractionInstrumentation } from "@opentelemetry/instrumentation-user-interaction";
import { XMLHttpRequestInstrumentation } from "@opentelemetry/instrumentation-xml-http-request";
import { FetchInstrumentation } from "@opentelemetry/instrumentation-fetch";
import { ZoneContextManager } from "@opentelemetry/context-zone";
import { registerInstrumentations } from "@opentelemetry/instrumentation";
import { resourceFromAttributes } from "@opentelemetry/resources";

/**
 * Initialize OpenTelemetry tracing for the browser application.
 * This should be called as early as possible in the application lifecycle.
 */
export function initializeOpenTelemetry(): void {
  // Create an OTLP exporter that sends traces to the OpenTelemetry Collector
  // via the Gateway YARP proxy at /api/otel/traces
  const exporter = new OTLPTraceExporter({
    url: "/api/otel/traces",
  });

  // Create a tracer provider with OTLP exporter
  const provider = new WebTracerProvider({
    spanProcessors: [new BatchSpanProcessor(exporter)],
    resource: resourceFromAttributes({
      "service.name": "bookmarks-react-ui",
      "user_agent.original": window.navigator.userAgent,
      "browser.language": window.navigator.language,
      "browser.mobile": (window.navigator as any).mobile,
      "browser.width": window.screen.width,
      "browser.height": window.screen.height,
      "http.hash": window.location.hash,
    }),
  });

  // Register the provider with the global API
  provider.register({
    // Use ZoneContextManager to properly handle asynchronous operations
    // This is important for tracking async operations like promises and timeouts
    contextManager: new ZoneContextManager(),
  });

  // Register browser instrumentations to automatically capture:
  // - Document load timing
  // - User interactions (clicks, etc.)
  // - XMLHttpRequest calls
  registerInstrumentations({
    instrumentations: [
      new DocumentLoadInstrumentation({
        applyCustomAttributesOnSpan: {
          documentLoad: (span) => {
            span.updateName(`documentLoad: ${window.location.pathname}`);
          },
          documentFetch: (span) => {
            span.updateName(`documentFetch: ${window.location.pathname}`);
          },
          resourceFetch: (span, resource) => {
            span.updateName(
              `resourceFetch: ${
                new URL(resource.name, window.location.origin).pathname
              }`
            );
            if ("renderBlockingStatus" in resource) {
              span.setAttribute(
                "resource.is_blocking",
                resource.renderBlockingStatus === "blocking"
              );
            }
          },
        },
      }),
      new UserInteractionInstrumentation({
        eventNames: ["click"],
        shouldPreventSpanCreation: (_eventName, element, span) => {
          span.setAttributes({
            "element.id": element.id,
            "element.className": element.className,
            "element.type": element.nodeName,
          });

          if (element instanceof HTMLInputElement) {
            span.setAttribute("element.input.value", element.value);
          }

          if (element instanceof HTMLAnchorElement) {
            span.setAttribute("element.link.href", element.href);
          }
          return false; // Always create spans for user interactions
        },
      }),
      new XMLHttpRequestInstrumentation(),
      new FetchInstrumentation(),
    ],
  });

  console.log(
    "OpenTelemetry browser instrumentation initialized with OTLP exporter"
  );
}
