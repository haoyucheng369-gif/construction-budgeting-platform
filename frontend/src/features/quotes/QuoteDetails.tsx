import type { Quote } from './api'

function formatAmount(amount: number, currency: string) {
  return new Intl.NumberFormat('fr-FR', { style: 'currency', currency }).format(amount)
}

export function QuoteDetails({ quote }: { quote: Quote }) {
  return (
    <>
      <p className="quote-meta">报价版本 {quote.version}</p>
      <p className="quote-total">
        销售总额：{formatAmount(quote.totalSalesAmount, quote.currency)}
      </p>
      {quote.lines.length === 0 ? (
        <p>这份报价还没有工程项。</p>
      ) : (
        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                <th scope="col">工程项</th>
                <th scope="col">工程量</th>
                <th scope="col">销售单价</th>
                <th scope="col">行金额</th>
              </tr>
            </thead>
            <tbody>
              {quote.lines.map((line) => (
                <tr key={line.lineId}>
                  <td>{line.description} <small>{line.workItemCode}</small></td>
                  <td>{line.quantity} {line.unit}</td>
                  <td>{formatAmount(line.salesUnitPrice, quote.currency)}</td>
                  <td>{formatAmount(line.lineAmount, quote.currency)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}
