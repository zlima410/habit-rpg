import React, { Component, ErrorInfo, ReactNode } from "react";
import ErrorBoundary from "./ErrorBoundary";

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
  resetOnPropsChange?: boolean;
  resetKeys?: Array<string | number>;
  enableReporting?: boolean;
}

class ErrorBoundaryWithReporting extends Component<Props> {
  private handleError = (error: Error, errorInfo: ErrorInfo) => {
    console.error("ErrorBoundary caught error:", {
      error: error.toString(),
      stack: error.stack,
      componentStack: errorInfo.componentStack,
      timestamp: new Date().toISOString(),
    });

    if (this.props.enableReporting && !__DEV__) {
      console.error("Error should be reported to monitoring service:", {
        error: error.message,
        stack: error.stack,
      });
    }
  };

  render() {
    return (
      <ErrorBoundary
        onError={this.handleError}
        fallback={this.props.fallback}
        resetOnPropsChange={this.props.resetOnPropsChange}
        resetKeys={this.props.resetKeys}
      >
        {this.props.children}
      </ErrorBoundary>
    );
  }
}

export default ErrorBoundaryWithReporting;