import { Component, type ReactNode } from 'react'

type Props = { children: ReactNode }
type State = { hasError: boolean; error?: Error }

export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error }
  }

  handleRetry = () => {
    this.setState({ hasError: false, error: undefined })
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="mx-auto flex min-h-[50vh] max-w-md flex-col items-center justify-center gap-4 px-4 text-center">
          <div className="rounded-full bg-rose-100 p-4">
            <svg className="h-8 w-8 text-rose-700" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v2m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <h2 className="font-display text-xl text-ink">Đã xảy ra lỗi</h2>
          <p className="text-sm text-muted">
            Trang gặp sự cố. Vui lòng thử lại hoặc quay về trang chủ.
          </p>
          <div className="flex gap-3">
            <button
              onClick={this.handleRetry}
              className="inline-flex items-center rounded-md bg-forest px-4 py-2 text-sm font-medium text-white hover:bg-forest-dark transition"
            >
              Thử lại
            </button>
            <a
              href="/"
              className="inline-flex items-center rounded-md bg-sand px-4 py-2 text-sm font-medium text-ink hover:bg-sand-dark border border-line transition"
            >
              Về trang chủ
            </a>
          </div>
        </div>
      )
    }
    return this.props.children
  }
}
