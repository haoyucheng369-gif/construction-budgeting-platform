import { useState } from 'react'
import { ProjectSelector } from '../features/projects'
import { QuotePanel } from '../features/quotes'

function App() {
  const [selectedProjectId, setSelectedProjectId] = useState('')

  return (
    <main className="page">
      <header className="page-header">
        <span className="eyebrow">Construction Budgeting</span>
        <h1>项目报价</h1>
        <p>选择要处理的工程项目。</p>
      </header>

      <ProjectSelector
        selectedProjectId={selectedProjectId}
        onSelectProject={setSelectedProjectId}
      />
      {selectedProjectId && <QuotePanel projectId={selectedProjectId} />}
    </main>
  )
}

export default App
