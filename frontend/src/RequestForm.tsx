import { useState, type FormEvent } from 'react';
import { api, type RequestInput, type WorkRequest } from './api';

type Props = { editing: WorkRequest | null; onSaved: () => void; onCancel: () => void };

const RequestForm = ({ editing, onSaved, onCancel }: Props) => {
  const [form, setForm] = useState<RequestInput>(editing ?? { title: '', description: '', department: '' });
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (saving) return;
    setError('');
    const data = { title: form.title.trim(), description: form.description.trim(), department: form.department.trim() };
    if (!data.title || !data.description || !data.department) {
      setError('Title, description, and department must not be blank.');
      return;
    }
    setSaving(true);
    try {
      await api.save(data, editing?.id);
      onSaved();
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Could not save the request.');
    } finally { setSaving(false); }
  };

  return <section className="panel form-panel" aria-labelledby="form-title">
    <div className="section-heading"><h2 id="form-title">{editing ? `Edit request · #${editing.id}` : 'New request'}</h2></div>
    <p className="muted">All fields are required.</p>
    <form onSubmit={submit} noValidate>
      <fieldset disabled={saving}>
        <label>Title<input required maxLength={120} value={form.title} onChange={e => setForm({ ...form, title: e.target.value })} placeholder="E.g. Meeting room projector issue" /></label>
        <label>Description<textarea required maxLength={2000} rows={5} value={form.description} onChange={e => setForm({ ...form, description: e.target.value })} placeholder="Describe your request or the issue" /></label>
        <label>Department<input required maxLength={80} value={form.department} onChange={e => setForm({ ...form, department: e.target.value })} placeholder="E.g. Information Technology" /></label>
        {error && <p role="alert" className="error">{error}</p>}
        <div className="actions"><button className="primary" type="submit">{saving ? 'Saving…' : editing ? 'Save changes' : 'Create request'}</button>
          {editing && <button type="button" onClick={onCancel}>Cancel</button>}</div>
      </fieldset>
    </form>
  </section>;
};

export default RequestForm;
