import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { getProjects } from './api/projects'

function App() {
  const [selectedProjectId, setSelectedProjectId] = useState('')
  const projectsQuery = useQuery({
    queryKey: ['projects'],
    queryFn: ({ signal }) => getProjects(signal),
  })
  const selectedProject = projectsQuery.data?.find(
    (project) => project.projectId === selectedProjectId,
  )

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
    </main>
  )
}

export default App
