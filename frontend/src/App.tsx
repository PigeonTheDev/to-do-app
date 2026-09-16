import { useEffect, useState } from 'react';
import { api, statusLabels, type Status, type WorkRequest } from './api';
import RequestForm from './RequestForm';

const App = () => {
  const [requests, setRequests] = useState<WorkRequest[]>([]);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [loading, setLoading] = useState(true);
  const [listError, setListError] = useState('');
  const [actionError, setActionError] = useState('');
  const [notice, setNotice] = useState('');
  const [editing, setEditing] = useState<WorkRequest | null>(null);
  const [revision, setRevision] = useState(0);
  const [formKey, setFormKey] = useState(0);
  const [busyId, setBusyId] = useState<number | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setListError('');
    const timer = setTimeout(() => {
      api.list(search, status, controller.signal)
        .then(data => { if (!controller.signal.aborted) setRequests(data); })
        .catch(error => { if (!controller.signal.aborted) setListError(error.message); })
        .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    }, 200);
    return () => { clearTimeout(timer); controller.abort(); };
  }, [search, status, revision]);

  const resetForm = () => { setEditing(null); setFormKey(key => key + 1); };
  const saved = () => {
    resetForm();
    setNotice('Request saved. Requests matching the active filters are displayed.');
    setRevision(value => value + 1);
  };
  const advance = async (request: WorkRequest) => {
    if (busyId !== null || request.status === 'Completed') return;
    setBusyId(request.id); setActionError(''); setNotice('');
    const next: Status = request.status === 'New' ? 'InProgress' : 'Completed';
    try {
      await api.save({ title: request.title, description: request.description, department: request.department, status: next }, request.id);
      setNotice(`Request #${request.id}: ${statusLabels[next]}.`);
      setRevision(value => value + 1);
    } catch (error) { setActionError(error instanceof Error ? error.message : 'Could not update the status.'); }
    finally { setBusyId(null); }
  };

  return <main>
    <header><div><p className="eyebrow">TEAM OPERATIONS</p><h1>Work Request Management</h1><p className="muted">Create, track, and complete work requests.</p></div><span className="workflow">New → In Progress → Completed</span></header>
    {notice && <p role="status" className="success">{notice}</p>}
    {actionError && <p role="alert" className="error">{actionError}</p>}
    <div className="layout">
      <RequestForm key={`${editing?.id ?? 'new'}-${formKey}`} editing={editing} onSaved={saved} onCancel={resetForm} />
      <section className="panel list-panel" aria-labelledby="list-title">
        <div className="section-heading"><h2 id="list-title">Requests</h2><button onClick={() => setRevision(value => value + 1)} disabled={loading}>Refresh</button></div>
        <div className="filters"><label>Search by title<input type="search" placeholder="Request title…" value={search} onChange={e => setSearch(e.target.value)} /></label>
          <label>Status<select value={status} onChange={e => setStatus(e.target.value)}><option value="">All statuses</option>{Object.entries(statusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label></div>
        {loading ? <p role="status" className="empty">Loading requests…</p> : listError ? <div role="alert" className="error">{listError}<br /><button onClick={() => setRevision(value => value + 1)}>Try again</button></div> : requests.length === 0 ? <p className="empty">{search || status ? 'No requests match your search or filter.' : 'No requests yet. Create your first request using the form.'}</p> : <>
          <p className="muted count">{requests.length} request(s) listed</p>
          <div className="request-list">{requests.map(request => <article key={request.id} className="request-card">
            <div className="card-top"><span className="request-id">#{request.id}</span><span className={`badge ${request.status}`}>{statusLabels[request.status]}</span></div>
            <h3>{request.title}</h3><p className="description">{request.description}</p>
            <div className="meta"><span>{request.department}</span><time dateTime={request.createdAt}>{new Date(request.createdAt).toLocaleDateString('en-US')}</time></div>
            <div className="actions"><button onClick={() => { setEditing(request); setActionError(''); document.getElementById('form-title')?.scrollIntoView({ behavior: 'smooth', block: 'start' }); }}>Edit</button>
              {request.status !== 'Completed' && <button className="advance" disabled={busyId !== null} onClick={() => void advance(request)}>{busyId === request.id ? 'Updating…' : request.status === 'New' ? 'Start work →' : 'Complete ✓'}</button>}</div>
          </article>)}</div>
        </>}
      </section>
    </div>
  </main>;
};

export default App;
