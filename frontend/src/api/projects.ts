export type Project = {
  projectId: string
  name: string
}

export async function getProjects(signal?: AbortSignal): Promise<Project[]> {
  const response = await fetch('/api/projects', { signal })

  if (!response.ok) {
    throw new Error(`无法读取项目列表（HTTP ${response.status}）`)
  }

  return (await response.json()) as Project[]
}
