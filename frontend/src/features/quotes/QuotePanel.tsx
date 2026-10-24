import { useQuery } from '@tanstack/react-query'
import { getQuoteByProject } from './api'
import { QuoteDetails } from './QuoteDetails'
import './styles.css'

export function QuotePanel({ projectId }: { projectId: string }) {
  const quoteQuery = useQuery({
    queryKey: ['quote', projectId],
    queryFn: ({ signal }) => getQuoteByProject(projectId, signal),
  })

  return (
    <section className="panel quote-panel" aria-labelledby="quote-heading">
      <h2 id="quote-heading">报价</h2>

      {quoteQuery.isPending && <p role="status">正在加载报价…</p>}

      {quoteQuery.isError && (
        <div role="alert">
          <p>{quoteQuery.error.message}</p>
          <button type="button" onClick={() => void quoteQuery.refetch()}>
            重试
          </button>
        </div>
      )}

      {quoteQuery.isSuccess && quoteQuery.data === null && (
        <p>该项目尚无报价。</p>
      )}

      {quoteQuery.isSuccess && quoteQuery.data !== null && (
        <QuoteDetails quote={quoteQuery.data} />
      )}
    </section>
  )
}
