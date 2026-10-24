import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { getProjects } from './api/projects'
import { getQuoteByProject, type Quote } from './api/quotes'

function formatAmount(amount: number, currency: string) {
  return new Intl.NumberFormat('fr-FR', { style: 'currency', currency }).format(amount)
}

function QuoteDetails({ quote }: { quote: Quote }) {
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

function App() {
  const [selectedProjectId, setSelectedProjectId] = useState('')
  const projectsQuery = useQuery({
    queryKey: ['projects'],
    queryFn: ({ signal }) => getProjects(signal),
  })
  const selectedProject = projectsQuery.data?.find(
    (project) => project.projectId === selectedProjectId,
  )
  const quoteQuery = useQuery({
    queryKey: ['quote', selectedProjectId],
    queryFn: ({ signal }) => getQuoteByProject(selectedProjectId, signal),
    enabled: selectedProject !== undefined,
  })

  return (
    <main className="page">
      <header className="page-header">
        <span className="eyebrow">Construction Budgeting</span>
        <h1>项目报价</h1>
        <p>选择要处理的工程项目。</p>
      </header>

      <section className="panel" aria-labelledby="projects-heading">
        <h2 id="projects-heading">项目列表</h2>

        {projectsQuery.isPending && <p role="status">正在加载项目…</p>}

        {projectsQuery.isError && (
          <div role="alert">
            <p>{projectsQuery.error.message}</p>
            <button type="button" onClick={() => void projectsQuery.refetch()}>
              重试
            </button>
          </div>
        )}

        {projectsQuery.isSuccess && projectsQuery.data.length === 0 && (
          <p>暂无项目。请先通过 Sales API 创建项目。</p>
        )}

        {projectsQuery.isSuccess && projectsQuery.data.length > 0 && (
          <>
            <label htmlFor="project">项目</label>
            <select
              id="project"
              value={selectedProjectId}
              onChange={(event) => setSelectedProjectId(event.target.value)}
            >
              <option value="">请选择项目</option>
              {projectsQuery.data.map((project) => (
                <option key={project.projectId} value={project.projectId}>
                  {project.name}
                </option>
              ))}
            </select>
          </>
        )}
      </section>

      {selectedProject && (
        <section className="panel" aria-labelledby="selected-heading">
          <h2 id="selected-heading">当前项目</h2>
          <p className="project-name">{selectedProject.name}</p>
          <p className="project-id">{selectedProject.projectId}</p>
        </section>
      )}

      {selectedProject && (
        <section className="panel" aria-labelledby="quote-heading">
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
      )}
    </main>
  )
}

export default App
