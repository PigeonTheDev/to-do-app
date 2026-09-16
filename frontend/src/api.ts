export type Status = 'New' | 'InProgress' | 'Completed';
export type RequestInput = { title: string; description: string; department: string; status?: Status };
export type WorkRequest = RequestInput & { id: number; status: Status; createdAt: string };

export const statusLabels: Record<Status, string> = { New: 'New', InProgress: 'In Progress', Completed: 'Completed' };

const request = async <T>(path: string, init?: RequestInit): Promise<T> => {
  let response: Response;
  try {
    response = await fetch(`/api/requests${path}`, {
      ...init,
      headers: { 'Content-Type': 'application/json', ...init?.headers },
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error;
    throw new Error('Unable to reach the server. Check your connection and make sure the API is running.');
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; errors?: Record<string, string[]> } | null;
    const details = problem?.errors ? Object.values(problem.errors).flat().join(' ') : '';
    throw new Error(details || problem?.title || 'The operation could not be completed. Please try again.');
  }
  return response.json() as Promise<T>;
};

export const api = {
  list: (search: string, status: string, signal: AbortSignal) => {
    const query = new URLSearchParams();
    if (search.trim()) query.set('search', search.trim());
    if (status) query.set('status', status);
    return request<WorkRequest[]>(`?${query}`, { signal });
  },
  save: (data: RequestInput, id?: number) => request<WorkRequest>(id ? `/${id}` : '', {
    method: id ? 'PUT' : 'POST', body: JSON.stringify(data),
  }),
};
