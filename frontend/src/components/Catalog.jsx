import React, { useEffect, useState } from 'react';
import api, { errorMessage } from '../api/api';
import { useAuth } from './AuthContext';
export default function Catalog({ categoriesOnly = false }) {
  const { can } = useAuth();
  const [items, setItems] = useState([]);
  const [categories, setCategories] = useState([]);
  const [query, setQuery] = useState('');
  const [filter, setFilter] = useState('');
  const [editor, setEditor] = useState(null);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const entity = categoriesOnly ? 'Category' : 'Product';

  async function load() {
    setLoading(true);
    setError('');
    try {
      const responses = await Promise.all([
        categoriesOnly ? api.post('Category/getallcategories') : api.get('Products/getall'),
        !categoriesOnly && can('Category.Get')
          ? api.post('Category/getallcategories')
          : Promise.resolve(null),
      ]);
      setItems(categoriesOnly ? responses[0].data.data : responses[0].data);
      if (responses[1]) setCategories(responses[1].data.data);
    } catch (requestError) {
      setError(errorMessage(requestError));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
    // The permission function is derived from the current session; category mode is the reload trigger.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [categoriesOnly]);

  useEffect(() => {
    if (!editor) return undefined;

    const previousOverflow = document.body.style.overflow;
    const closeOnEscape = event => {
      if (event.key === 'Escape' && !busy) {
        setEditor(null);
        setError('');
      }
    };

    document.body.style.overflow = 'hidden';
    document.addEventListener('keydown', closeOnEscape);
    return () => {
      document.body.style.overflow = previousOverflow;
      document.removeEventListener('keydown', closeOnEscape);
    };
  }, [editor, busy]);

  function closeEditor() {
    if (busy) return;
    setEditor(null);
    setError('');
  }

  async function save(event) {
    event.preventDefault();
    setBusy(true);
    setError('');
    setNotice('');

    const data = Object.fromEntries(new FormData(event.currentTarget));
    if (!categoriesOnly) {
      ['categoryId', 'unitPrice', 'unitsInStock'].forEach(key => {
        data[key] = Number(data[key]);
      });
    }
    if (editor.id) data.id = editor.id;

    try {
      const endpoint = categoriesOnly
        ? `Category/${editor.id ? 'updatecategory' : 'addcategory'}`
        : `Products/${editor.id ? 'update' : 'add'}`;
      await api.post(endpoint, data);
      setEditor(null);
      await load();
      setNotice('Record saved.');
    } catch (requestError) {
      setError(errorMessage(requestError));
    } finally {
      setBusy(false);
    }
  }

  async function remove(item) {
    const name = item.productName || item.categoryName;
    if (!window.confirm(`Delete “${name}”?`)) return;

    setBusy(true);
    setNotice('');
    setError('');
    try {
      await api.delete(`${categoriesOnly ? 'Category' : 'Products'}/${item.id}`);
      await load();
      setNotice('Record deleted.');
    } catch (requestError) {
      setError(errorMessage(requestError));
    } finally {
      setBusy(false);
    }
  }

  const visible = items.filter(item => {
    const name = item.productName || item.categoryName;
    return name.toLocaleLowerCase('en-US').includes(query.toLocaleLowerCase('en-US'))
      && (!filter || String(item.categoryId) === filter);
  });
  const canManage = can(`${entity}.Update`) || can(`${entity}.Delete`);

  return (
    <>
      <div className="page-heading">
        <div>
          <span className="eyebrow">CATALOG</span>
          <h1>{categoriesOnly ? 'Categories' : 'Products'}</h1>
          <p className="muted">
            {categoriesOnly ? 'Organize the categories in your catalog.' : 'Explore product, pricing, and inventory information.'}
          </p>
        </div>
        {can(`${entity}.Add`) && (
          <button disabled={busy} onClick={() => { setEditor({}); setError(''); }}>
            + Add {categoriesOnly ? 'category' : 'product'}
          </button>
        )}
      </div>

      {!editor && error && <p role="alert" className="error">{error}</p>}
      {notice && <p role="status" className="success">{notice}</p>}

      {editor && (
        <div
          className="modal-backdrop"
          onMouseDown={event => {
            if (event.target === event.currentTarget) closeEditor();
          }}
        >
          <section
            className="panel modal-panel"
            role="dialog"
            aria-modal="true"
            aria-labelledby="editor-title"
          >
            <div className="modal-header">
              <div>
                <span className="eyebrow">{categoriesOnly ? 'CATEGORY' : 'PRODUCT'}</span>
                <h2 id="editor-title">{`${editor.id ? 'Edit' : 'Add'} ${categoriesOnly ? 'category' : 'product'}`}</h2>
              </div>
              <button
                className="modal-close secondary"
                type="button"
                disabled={busy}
                aria-label="Close dialog"
                onClick={closeEditor}
              >
                ×
              </button>
            </div>
            {error && <p role="alert" className="error">{error}</p>}
            <form onSubmit={save}>
              <div className="form-grid">
                <label>
                  {categoriesOnly ? 'Category name' : 'Product name'}
                  <input
                    autoFocus
                    name={categoriesOnly ? 'categoryName' : 'productName'}
                    required
                    minLength={categoriesOnly ? 1 : 2}
                    maxLength={categoriesOnly ? 100 : 30}
                    defaultValue={editor.categoryName || editor.productName || ''}
                  />
                </label>
                {!categoriesOnly && (
                  <>
                    <label>
                      Category
                      {can('Category.Get') ? (
                        <select name="categoryId" required defaultValue={editor.categoryId || ''}>
                          <option value="">Select a category</option>
                          {categories.map(category => (
                            <option key={category.id} value={category.id}>{category.categoryName}</option>
                          ))}
                        </select>
                      ) : (
                        <input name="categoryId" type="number" min="1" required defaultValue={editor.categoryId} />
                      )}
                    </label>
                    <label>
                      Unit description
                      <input name="quantityPerUnit" defaultValue={editor.quantityPerUnit || ''} />
                    </label>
                    <label>
                      Unit price
                      <input name="unitPrice" type="number" min="1" step="0.01" required defaultValue={editor.unitPrice} />
                    </label>
                    <label>
                      Units in stock
                      <input name="unitsInStock" type="number" min="0" max="32767" step="1" required defaultValue={editor.unitsInStock ?? 0} />
                    </label>
                  </>
                )}
              </div>
              <div className="actions">
                <button disabled={busy}>{busy ? 'Saving…' : 'Save'}</button>
                <button className="secondary" type="button" disabled={busy} onClick={closeEditor}>Cancel</button>
              </div>
            </form>
          </section>
        </div>
      )}

      <section className="panel">
        <div className="filters">
          <label>
            Search
            <input placeholder="Search by name…" value={query} onChange={event => setQuery(event.target.value)} />
          </label>
          {!categoriesOnly && can('Category.Get') && (
            <label>
              Category
              <select value={filter} onChange={event => setFilter(event.target.value)}>
                <option value="">All categories</option>
                {categories.map(category => (
                  <option key={category.id} value={category.id}>{category.categoryName}</option>
                ))}
              </select>
            </label>
          )}
          <button className="secondary" disabled={loading || busy} onClick={load}>Refresh</button>
        </div>

        {loading ? (
          <p role="status">Loading…</p>
        ) : (
          <>
            <p className="muted">{visible.length} {visible.length === 1 ? 'record' : 'records'}</p>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Name</th>
                    {!categoriesOnly && <><th>Category</th><th>Unit</th><th>Price</th><th>Stock</th></>}
                    {canManage && <th>Actions</th>}
                  </tr>
                </thead>
                <tbody>
                  {visible.map(item => (
                    <tr key={item.id}>
                      <td>{item.productName || item.categoryName}</td>
                      {!categoriesOnly && (
                        <>
                          <td>{item.category?.categoryName || categories.find(category => category.id === item.categoryId)?.categoryName || item.categoryId}</td>
                          <td>{item.quantityPerUnit || '—'}</td>
                          <td>{new Intl.NumberFormat('en-US', { minimumFractionDigits: 2 }).format(item.unitPrice)}</td>
                          <td><span className="badge">{item.unitsInStock === 0 ? 'Out of stock' : item.unitsInStock}</span></td>
                        </>
                      )}
                      {canManage && (
                        <td>
                          <div className="actions">
                            {can(`${entity}.Update`) && (
                              <button className="secondary" disabled={busy} onClick={() => { setEditor(item); setError(''); }}>Edit</button>
                            )}
                            {can(`${entity}.Delete`) && (
                              <button className="danger" disabled={busy} onClick={() => remove(item)}>Delete</button>
                            )}
                          </div>
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {visible.length === 0 && <p>No records found.</p>}
          </>
        )}
      </section>
    </>
  );
}
