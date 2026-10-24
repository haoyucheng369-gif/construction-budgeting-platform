export type QuoteLine = {
  lineId: string
  workItemCode: string
  description: string
  unit: string
  quantity: number
  salesUnitPrice: number
  lineAmount: number
}

export type Quote = {
  quoteId: string
  projectId: string
  version: number
  totalSalesAmount: number
  currency: string
  lines: QuoteLine[]
}

export async function getQuoteByProject(
  projectId: string,
  signal?: AbortSignal,
): Promise<Quote | null> {
  const response = await fetch(`/api/quotes/by-project/${encodeURIComponent(projectId)}`, { signal })

  if (response.status === 204) {
    return null
  }

  if (!response.ok) {
    throw new Error(`无法读取报价（HTTP ${response.status}）`)
  }

  return (await response.json()) as Quote
}
