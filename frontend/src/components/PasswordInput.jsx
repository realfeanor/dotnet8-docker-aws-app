import React, { useId, useState } from 'react';

export default function PasswordInput({ label, name, autoComplete }) {
  const id = useId();
  const [visible, setVisible] = useState(false);

  return (
    <div>
      <label htmlFor={id}>{label}</label>
      <div className="password-field">
        <input id={id} name={name} type={visible ? 'text' : 'password'} required autoComplete={autoComplete} />
        <button type="button" className="secondary" aria-controls={id} aria-pressed={visible}
          aria-label={`${visible ? 'Hide' : 'Show'} ${label.toLowerCase()}`} onClick={() => setVisible(value => !value)}>
          {visible ? 'Hide' : 'Show'}
        </button>
      </div>
    </div>
  );
}
