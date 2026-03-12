import { useEffect, useRef, useState } from 'react'
import { apiFetch, readErrorMessage } from '../lib/api'
import './ProfilePage.css'

type ProfilePageProps = {
  token: string
}

type ContactPerson = {
  id: number
  name: string
  relation: string
  phone: string
  email: string
}

type ProfileData = {
  id: number
  firstName: string
  lastName: string
  email?: string | null
  phone?: string | null
  address?: string | null
  contactPersons: ContactPerson[]
}

export default function ProfilePage({ token }: ProfilePageProps) {
  const [profile, setProfile] = useState<ProfileData | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [notification, setNotification] = useState<{
    message: string
    type: 'success' | 'error'
    visible: boolean
  } | null>(null)
  const hideTimerRef = useRef<number | null>(null)
  const clearTimerRef = useRef<number | null>(null)

  const showNotification = (message: string, type: 'success' | 'error') => {
    if (hideTimerRef.current) {
      window.clearTimeout(hideTimerRef.current)
    }
    if (clearTimerRef.current) {
      window.clearTimeout(clearTimerRef.current)
    }

    setNotification({ message, type, visible: true })
    hideTimerRef.current = window.setTimeout(() => {
      setNotification((current) =>
        current ? { ...current, visible: false } : current
      )
    }, 9000)
    clearTimerRef.current = window.setTimeout(() => {
      setNotification(null)
    }, 10000)
  }

  const loadProfile = async () => {
    setLoading(true)
    setError(null)
    try {
      const response = await apiFetch('/api/employees/me', {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      })
      if (!response.ok) {
        const errorMessage = await readErrorMessage(response)
        setError(errorMessage)
        return
      }
      const data = (await response.json()) as any
      if (!data) {
        setError('No profile data found')
        return
      }
      const contactPersons = Array.isArray(data.contactPersons)
        ? data.contactPersons.map((contact: any) => ({
            id: contact.id ?? Date.now(),
            name: contact.name ?? '',
            relation: contact.relationship ?? contact.relation ?? '',
            phone: contact.phone ?? '',
            email: contact.email ?? '',
          }))
        : []

      setProfile({
        id: data.id,
        firstName: data.firstName ?? '',
        lastName: data.lastName ?? '',
        email: data.email ?? '',
        phone: data.phone ?? '',
        address: data.address ?? '',
        contactPersons,
      })
    } catch {
      setError('Failed to load profile')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadProfile()
  }, [token])

  const handleBasicChange = (
    field: 'email' | 'phone' | 'address',
    value: string
  ) => {
    setProfile((prev) =>
      prev ? { ...prev, [field]: value } : prev
    )
  }

  const saveProfile = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!profile) return

    setSaving(true)
    try {
      const response = await apiFetch('/api/employees/me', {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          email: profile.email || null,
          phone: profile.phone || null,
          address: profile.address || null,
        }),
      })

      if (!response.ok) {
        const errorMessage = await readErrorMessage(response)
        showNotification(errorMessage, 'error')
        return
      }

      showNotification('Profile updated successfully', 'success')
    } catch {
      showNotification('Failed to update profile', 'error')
    } finally {
      setSaving(false)
    }
  }

  const addContact = async (contact: ContactPerson) => {
    if (!profile) return
    const response = await apiFetch(`/api/employees/${profile.id}/contacts`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({
        id: 0,
        employeeId: profile.id,
        name: contact.name,
        relationship: contact.relation,
        phone: contact.phone,
        email: contact.email,
        isPrimary: false,
      }),
    })

    if (!response.ok) {
      const errorMessage = await readErrorMessage(response)
      showNotification(errorMessage, 'error')
      return
    }

    const saved = await response.json()
    setProfile((prev) =>
      prev
        ? {
            ...prev,
            contactPersons: [
              ...prev.contactPersons,
              {
                id: saved.id ?? contact.id,
                name: saved.name ?? contact.name,
                relation: saved.relationship ?? contact.relation,
                phone: saved.phone ?? contact.phone,
                email: saved.email ?? contact.email,
              },
            ],
          }
        : prev
    )
    showNotification('Contact added', 'success')
  }

  const updateContact = async (contact: ContactPerson) => {
    if (!profile) return
    const response = await apiFetch(
      `/api/employees/${profile.id}/contacts/${contact.id}`,
      {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          id: contact.id,
          employeeId: profile.id,
          name: contact.name,
          relationship: contact.relation,
          phone: contact.phone,
          email: contact.email,
          isPrimary: false,
        }),
      }
    )

    if (!response.ok) {
      const errorMessage = await readErrorMessage(response)
      showNotification(errorMessage, 'error')
      return
    }

    setProfile((prev) =>
      prev
        ? {
            ...prev,
            contactPersons: prev.contactPersons.map((item) =>
              item.id === contact.id ? contact : item
            ),
          }
        : prev
    )
    showNotification('Contact updated', 'success')
  }

  const deleteContact = async (contactId: number) => {
    if (!profile) return
    const response = await apiFetch(
      `/api/employees/${profile.id}/contacts/${contactId}`,
      {
        method: 'DELETE',
        headers: {
          Authorization: `Bearer ${token}`,
        },
      }
    )

    if (!response.ok) {
      const errorMessage = await readErrorMessage(response)
      showNotification(errorMessage, 'error')
      return
    }

    setProfile((prev) =>
      prev
        ? {
            ...prev,
            contactPersons: prev.contactPersons.filter(
              (item) => item.id !== contactId
            ),
          }
        : prev
    )
    showNotification('Contact deleted', 'success')
  }

  if (loading) {
    return <div className="profile-page">Loading...</div>
  }

  if (error) {
    return (
      <div className="profile-page">
        <div className="profile-card">
          <p>{error}</p>
          <button type="button" className="primary-btn" onClick={loadProfile}>
            Retry
          </button>
        </div>
      </div>
    )
  }

  if (!profile) {
    return <div className="profile-page">No profile data.</div>
  }

  return (
    <div className="profile-page">
      {notification && (
        <div
          className={`notification ${notification.type} ${
            notification.visible ? 'show' : ''
          }`}
        >
          {notification.message}
        </div>
      )}

      <form className="profile-card" onSubmit={saveProfile}>
        <h3>Basic Information</h3>

        <div className="profile-row">
          <div className="profile-field">
            <label>First Name</label>
            <input value={profile.firstName} readOnly />
          </div>
          <div className="profile-field">
            <label>Last Name</label>
            <input value={profile.lastName} readOnly />
          </div>
        </div>

        <div className="profile-row">
          <div className="profile-field">
            <label>Email</label>
            <input
              value={profile.email ?? ''}
              onChange={(e) => handleBasicChange('email', e.target.value)}
            />
          </div>
          <div className="profile-field">
            <label>Phone</label>
            <input
              value={profile.phone ?? ''}
              onChange={(e) => handleBasicChange('phone', e.target.value)}
            />
          </div>
        </div>

        <div className="profile-field">
          <label>Address</label>
          <input
            value={profile.address ?? ''}
            onChange={(e) => handleBasicChange('address', e.target.value)}
          />
        </div>

        <button type="submit" className="primary-btn" disabled={saving}>
          Save Profile
        </button>
      </form>

      <ProfileContacts
        contacts={profile.contactPersons}
        onAdd={addContact}
        onUpdate={updateContact}
        onDelete={deleteContact}
      />
    </div>
  )
}

function ProfileContacts({
  contacts,
  onAdd,
  onUpdate,
  onDelete,
}: {
  contacts: ContactPerson[]
  onAdd: (contact: ContactPerson) => void
  onUpdate: (contact: ContactPerson) => void
  onDelete: (id: number) => void
}) {
  const [editingId, setEditingId] = useState<number | null>(null)
  const [form, setForm] = useState({
    name: '',
    relation: '',
    phone: '',
    email: '',
  })

  const resetForm = () => {
    setForm({
      name: '',
      relation: '',
      phone: '',
      email: '',
    })
    setEditingId(null)
  }

  const handleSubmit = () => {
    if (!form.name) return
    if (editingId === null) {
      onAdd({ id: Date.now(), ...form })
    } else {
      onUpdate({ id: editingId, ...form })
    }
    resetForm()
  }

  const handleEdit = (contact: ContactPerson) => {
    setForm({
      name: contact.name,
      relation: contact.relation,
      phone: contact.phone,
      email: contact.email,
    })
    setEditingId(contact.id)
  }

  return (
    <div className="profile-card">
      <h3>Contact Persons</h3>

      <div className="contact-form">
        <input
          placeholder="Name"
          value={form.name}
          onChange={(e) => setForm({ ...form, name: e.target.value })}
        />
        <input
          placeholder="Relation"
          value={form.relation}
          onChange={(e) => setForm({ ...form, relation: e.target.value })}
        />
        <input
          placeholder="Phone"
          value={form.phone}
          onChange={(e) => setForm({ ...form, phone: e.target.value })}
        />
        <input
          placeholder="Email"
          value={form.email}
          onChange={(e) => setForm({ ...form, email: e.target.value })}
        />

        {editingId === null ? (
          <button type="button" className="add-btn" onClick={handleSubmit}>
            Add
          </button>
        ) : (
          <>
            <button type="button" className="update-btn" onClick={handleSubmit}>
              Update
            </button>
            <button type="button" className="cancel-btn" onClick={resetForm}>
              Cancel
            </button>
          </>
        )}
      </div>

      {contacts.length > 0 && (
        <table className="contact-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Relation</th>
              <th>Phone</th>
              <th>Email</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {contacts.map((contact) => (
              <tr key={contact.id}>
                <td>{contact.name}</td>
                <td>{contact.relation}</td>
                <td>{contact.phone}</td>
                <td>{contact.email}</td>
                <td className="employee-actions">
                  <button
                    type="button"
                    className="edit-btn"
                    onClick={() => handleEdit(contact)}
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    className="delete-btn"
                    onClick={() => onDelete(contact.id)}
                  >
                    Delete
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
