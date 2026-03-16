import { useEffect, useRef, useState } from "react"
import type { FormEvent, ReactNode } from "react"
import { apiFetch } from "../../lib/api"
import "../../pages/EmployeePage.css"

export type ContactPerson = {
  id: number
  name: string
  relation: string
  phone: string
  email: string
}

export type Employee = {
  id: number
  firstName: string
  lastName: string
  email?: string | null
  phone?: string | null
  dateOfBirth?: string | null
  department?: string | null
  position?: string | null
  basicSalary?: number
  managerId?: number | null
  dateOfJoining?: string | null
  address?: string | null
  bankName?: string | null
  bankAccountNumber?: string | null
  ifscCode?: string | null
  totalSickLeaves?: number
  usedSickLeaves?: number
  totalCasualLeaves?: number
  usedCasualLeaves?: number
  totalGovernmentLeaves?: number
  usedGovernmentLeaves?: number
  totalUnpaidLeaves?: number
  usedUnpaidLeaves?: number
  contactPersons?: ContactPerson[]
}

type EmployeeFormProps = {
  employee: Employee | null
  token: string
  onNotify: (message: string, type: "success" | "error") => void
  onSaved: () => void
  onCancel: () => void
}

type EmployeeFormState = {
  employeeId: string
  firstName: string
  lastName: string
  email: string
  phone: string
  dateOfBirth: string
  department: string
  position: string
  managerId: string
  basicSalary: string
  dateOfJoining: string
  address: string
  bankName: string
  bankAccountNumber: string
  ifscCode: string
  totalSickLeaves: string
  totalCasualLeaves: string
  totalGovernmentLeaves: string
  totalUnpaidLeaves: string
  createLogin: boolean
  username: string
  password: string
  roleName: string
}

const createInitialForm = (employee: Employee | null): EmployeeFormState => ({
  employeeId: employee?.id ? String(employee.id) : "Auto-generated",
  firstName: employee?.firstName ?? "",
  lastName: employee?.lastName ?? "",
  email: employee?.email ?? "",
  phone: employee?.phone ?? "",
  dateOfBirth: employee?.dateOfBirth ?? "",
  department: employee?.department ?? "",
  position: employee?.position ?? "",
  managerId: employee?.managerId ? String(employee.managerId) : "",
  basicSalary: employee?.basicSalary ? String(employee.basicSalary) : "",
  dateOfJoining: employee?.dateOfJoining ?? "",
  address: employee?.address ?? "",
  bankName: employee?.bankName ?? "",
  bankAccountNumber: employee?.bankAccountNumber ?? "",
  ifscCode: employee?.ifscCode ?? "",
  totalSickLeaves: employee?.totalSickLeaves ? String(employee.totalSickLeaves) : "",
  totalCasualLeaves: employee?.totalCasualLeaves ? String(employee.totalCasualLeaves) : "",
  totalGovernmentLeaves: employee?.totalGovernmentLeaves ? String(employee.totalGovernmentLeaves) : "",
  totalUnpaidLeaves: employee?.totalUnpaidLeaves ? String(employee.totalUnpaidLeaves) : "",
  createLogin: false,
  username: "",
  password: "",
  roleName: ""
})

async function getErrorMessage(response: Response) {
  try {
    const data = await response.json()
    if (data && typeof data === "object") {
      if ("message" in data && typeof data.message === "string") {
        return data.message
      }
      return JSON.stringify(data)
    }
  } catch {
    // ignore JSON parsing errors
  }

  try {
    const text = await response.text()
    return text || `Request failed (${response.status})`
  } catch {
    return `Request failed (${response.status})`
  }
}

function FormField({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="form-field">
      <label>{label}</label>
      {children}
    </div>
  )
}

export default function EmployeeForm({
  employee,
  token,
  onNotify,
  onSaved,
  onCancel
}: EmployeeFormProps) {
  const [activeSection, setActiveSection] = useState("basic")
  const formRef = useRef<HTMLDivElement | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [form, setForm] = useState<EmployeeFormState>(() => createInitialForm(employee))
  const [contacts, setContacts] = useState<ContactPerson[]>(employee?.contactPersons ?? [])

  useEffect(() => {
    setForm(createInitialForm(employee))
    setContacts(employee?.contactPersons ?? [])
  }, [employee])

  useEffect(() => {
    const sectionIds = ["basic-section", "job-section", "payroll-section", "contact-section"]
    const container = formRef.current
    if (!container) return

    const handleScroll = () => {
      const triggerPoint = container.clientHeight * 0.25
      let currentSection = "basic"

      sectionIds.forEach((id) => {
        const element = document.getElementById(id)
        if (!element) return

        const rect = element.getBoundingClientRect()
        const containerRect = container.getBoundingClientRect()
        const offset = rect.top - containerRect.top

        if (offset <= triggerPoint) {
          currentSection = id.replace("-section", "")
        }
      })

      setActiveSection(currentSection)
    }

    container.addEventListener("scroll", handleScroll)
    handleScroll()

    return () => container.removeEventListener("scroll", handleScroll)
  }, [])

  const handleChange = (field: keyof EmployeeFormState, value: string | boolean) => {
    setForm((current) => ({ ...current, [field]: value }))
  }

  const scrollTo = (id: string) => {
    const element = document.getElementById(id)
    if (element) {
      element.scrollIntoView({ behavior: "smooth", block: "start" })
    }
  }

  const toNumber = (value: string) => {
    const parsed = Number(value)
    return Number.isNaN(parsed) ? 0 : parsed
  }

  const toNumberOrNull = (value: string) => {
    if (!value) return null
    const parsed = Number(value)
    return Number.isNaN(parsed) ? null : parsed
  }

  const syncContactsForEdit = async (employeeId: number) => {
    const originalContacts = employee?.contactPersons ?? []
    const originalById = new Map(originalContacts.map((contact) => [contact.id, contact]))
    const currentExisting = contacts.filter((contact) => originalById.has(contact.id))
    const currentNew = contacts.filter((contact) => !originalById.has(contact.id))
    const deletedContacts = originalContacts.filter(
      (original) => !contacts.some((contact) => contact.id === original.id)
    )

    for (const contact of currentNew) {
      const response = await apiFetch(`/api/employees/${employeeId}/contacts`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({
          id: 0,
          employeeId,
          name: contact.name,
          relationship: contact.relation,
          phone: contact.phone,
          email: contact.email,
          isPrimary: false
        })
      })

      if (!response.ok) {
        throw new Error(await getErrorMessage(response))
      }
    }

    for (const contact of currentExisting) {
      const original = originalById.get(contact.id)
      if (!original) continue

      const changed =
        original.name !== contact.name ||
        original.relation !== contact.relation ||
        original.phone !== contact.phone ||
        original.email !== contact.email

      if (!changed) continue

      const response = await apiFetch(`/api/employees/${employeeId}/contacts/${contact.id}`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({
          id: contact.id,
          employeeId,
          name: contact.name,
          relationship: contact.relation,
          phone: contact.phone,
          email: contact.email,
          isPrimary: false
        })
      })

      if (!response.ok) {
        throw new Error(await getErrorMessage(response))
      }
    }

    for (const contact of deletedContacts) {
      const response = await apiFetch(`/api/employees/${employeeId}/contacts/${contact.id}`, {
        method: "DELETE",
        headers: {
          Authorization: `Bearer ${token}`
        }
      })

      if (!response.ok) {
        throw new Error(await getErrorMessage(response))
      }
    }
  }

  const buildEmployeePayload = () => ({
    id: employee?.id ?? 0,
    firstName: form.firstName,
    lastName: form.lastName,
    email: form.email || null,
    phone: form.phone || null,
    dateOfBirth: form.dateOfBirth || null,
    department: form.department || null,
    position: form.position || null,
    basicSalary: toNumber(form.basicSalary),
    managerId: toNumberOrNull(form.managerId),
    dateOfJoining: form.dateOfJoining || null,
    address: form.address || null,
    bankName: form.bankName || null,
    bankAccountNumber: form.bankAccountNumber || null,
    ifscCode: form.ifscCode || null,
    totalSickLeaves: toNumber(form.totalSickLeaves),
    usedSickLeaves: employee?.usedSickLeaves ?? 0,
    totalCasualLeaves: toNumber(form.totalCasualLeaves),
    usedCasualLeaves: employee?.usedCasualLeaves ?? 0,
    totalGovernmentLeaves: toNumber(form.totalGovernmentLeaves),
    usedGovernmentLeaves: employee?.usedGovernmentLeaves ?? 0,
    totalUnpaidLeaves: toNumber(form.totalUnpaidLeaves),
    usedUnpaidLeaves: employee?.usedUnpaidLeaves ?? 0,
    createLogin: form.createLogin,
    username: form.createLogin ? form.username : null,
    password: form.createLogin ? form.password : null,
    roleName: form.createLogin ? form.roleName : null,
    contactPersons: contacts.map((contact) => ({
      id: employee ? contact.id : 0,
      employeeId: employee?.id ?? 0,
      name: contact.name,
      relationship: contact.relation,
      phone: contact.phone,
      email: contact.email,
      isPrimary: false
    }))
  })

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!form.firstName || !form.lastName) {
      onNotify("Please fill required fields", "error")
      return
    }

    if (form.createLogin && (!form.username || !form.password)) {
      onNotify("Login ID and password are required", "error")
      return
    }

    setSubmitting(true)
    try {
      const payload = buildEmployeePayload()

      if (employee) {
        const response = await apiFetch(`/api/employees/${employee.id}`, {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`
          },
          body: JSON.stringify(payload)
        })

        if (!response.ok) {
          onNotify(await getErrorMessage(response), "error")
          return
        }

        await syncContactsForEdit(employee.id)
        onNotify("Employee updated successfully", "success")
        onSaved()
        return
      }

      const response = await apiFetch("/api/employees", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify(payload)
      })

      if (!response.ok) {
        onNotify(await getErrorMessage(response), "error")
        return
      }

      onNotify("Employee saved successfully", "success")
      onSaved()
    } catch (error) {
      onNotify(error instanceof Error ? error.message : "Failed to save employee", "error")
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="employee-layout">
      <aside className="employee-submenu">
        <button
          className={`submenu-item ${activeSection === "basic" ? "active" : ""}`}
          onClick={() => scrollTo("basic-section")}
        >
          Basic Information
        </button>
        <button
          className={`submenu-item ${activeSection === "job" ? "active" : ""}`}
          onClick={() => scrollTo("job-section")}
        >
          Job Details
        </button>
        <button
          className={`submenu-item ${activeSection === "payroll" ? "active" : ""}`}
          onClick={() => scrollTo("payroll-section")}
        >
          Payroll Details
        </button>
        <button
          className={`submenu-item ${activeSection === "contact" ? "active" : ""}`}
          onClick={() => scrollTo("contact-section")}
        >
          Contact Persons
        </button>
      </aside>

      <section className="employee-info">
        {activeSection === "basic" && (
          <>
            <h3>Basic Information</h3>
            <p>Enter personal employee details.</p>
          </>
        )}

        {activeSection === "job" && (
          <>
            <h3>Job Details</h3>
            <p>Configure employee department and role.</p>
          </>
        )}

        {activeSection === "payroll" && (
          <>
            <h3>Payroll Details</h3>
            <p>Salary and bank information.</p>
          </>
        )}

        {activeSection === "contact" && (
          <>
            <h3>Contact Persons</h3>
            <p>Emergency and alternate contacts.</p>
          </>
        )}
      </section>

      <section className="employee-form" ref={formRef}>
        <form className="employee-form-grid" onSubmit={handleSubmit}>
          <div id="basic-section" className="form-section">
            <h3>Basic Information</h3>

            <FormField label="Employee ID">
              <input value={form.employeeId} readOnly disabled />
            </FormField>

            <FormField label="First Name">
              <input value={form.firstName} onChange={(event) => handleChange("firstName", event.target.value)} required />
            </FormField>

            <FormField label="Last Name">
              <input value={form.lastName} onChange={(event) => handleChange("lastName", event.target.value)} required />
            </FormField>

            <FormField label="Email">
              <input type="email" value={form.email} onChange={(event) => handleChange("email", event.target.value)} />
            </FormField>

            <FormField label="Phone">
              <input value={form.phone} onChange={(event) => handleChange("phone", event.target.value)} />
            </FormField>

            <FormField label="Date of Birth">
              <input type="date" value={form.dateOfBirth} onChange={(event) => handleChange("dateOfBirth", event.target.value)} />
            </FormField>

            <FormField label="Address">
              <input value={form.address} onChange={(event) => handleChange("address", event.target.value)} />
            </FormField>
          </div>

          <div id="job-section" className="form-section">
            <h3>Job Details</h3>

            <FormField label="Department">
              <input value={form.department} onChange={(event) => handleChange("department", event.target.value)} />
            </FormField>

            <FormField label="Role">
              <input value={form.position} onChange={(event) => handleChange("position", event.target.value)} />
            </FormField>

            <FormField label="Manager ID">
              <input value={form.managerId} onChange={(event) => handleChange("managerId", event.target.value)} />
            </FormField>

            <FormField label="Date of Joining">
              <input type="date" value={form.dateOfJoining} onChange={(event) => handleChange("dateOfJoining", event.target.value)} />
            </FormField>
          </div>

          <div id="payroll-section" className="form-section">
            <h3>Payroll Details</h3>

            <FormField label="Salary">
              <input type="number" min="0" value={form.basicSalary} onChange={(event) => handleChange("basicSalary", event.target.value)} />
            </FormField>

            <FormField label="Bank Name">
              <input value={form.bankName} onChange={(event) => handleChange("bankName", event.target.value)} />
            </FormField>

            <FormField label="Bank Account">
              <input value={form.bankAccountNumber} onChange={(event) => handleChange("bankAccountNumber", event.target.value)} />
            </FormField>

            <FormField label="IFSC">
              <input value={form.ifscCode} onChange={(event) => handleChange("ifscCode", event.target.value)} />
            </FormField>

            <FormField label="Total Sick Leaves">
              <input type="number" min="0" value={form.totalSickLeaves} onChange={(event) => handleChange("totalSickLeaves", event.target.value)} />
            </FormField>

            <FormField label="Total Casual Leaves">
              <input type="number" min="0" value={form.totalCasualLeaves} onChange={(event) => handleChange("totalCasualLeaves", event.target.value)} />
            </FormField>

            <FormField label="Total Government Leaves">
              <input type="number" min="0" value={form.totalGovernmentLeaves} onChange={(event) => handleChange("totalGovernmentLeaves", event.target.value)} />
            </FormField>

            <FormField label="Total Unpaid Leaves">
              <input type="number" min="0" value={form.totalUnpaidLeaves} onChange={(event) => handleChange("totalUnpaidLeaves", event.target.value)} />
            </FormField>

            <FormField label="Create Login">
              <input type="checkbox" checked={form.createLogin} onChange={(event) => handleChange("createLogin", event.target.checked)} />
            </FormField>

            {form.createLogin && (
              <>
                <FormField label="Login ID">
                  <input value={form.username} onChange={(event) => handleChange("username", event.target.value)} required />
                </FormField>

                <FormField label="Login Password">
                  <input type="password" value={form.password} onChange={(event) => handleChange("password", event.target.value)} required />
                </FormField>

                <FormField label="Role">
                  <input value={form.roleName} onChange={(event) => handleChange("roleName", event.target.value)} />
                </FormField>
              </>
            )}
          </div>

          <div id="contact-section" className="form-section">
            <ContactPersonSection contacts={contacts} onChange={setContacts} />
          </div>

          <div className="form-actions">
            <button type="submit" className="primary-btn-1" disabled={submitting}>
              {employee ? "Edit Employee" : "Save Employee"}
            </button>
            <button type="button" className="cancel-btn" onClick={onCancel}>
              Cancel
            </button>
          </div>
        </form>
      </section>
    </div>
  )
}

function ContactPersonSection({
  contacts,
  onChange
}: {
  contacts: ContactPerson[]
  onChange: (contacts: ContactPerson[]) => void
}) {
  const [editingId, setEditingId] = useState<number | null>(null)
  const [form, setForm] = useState({
    name: "",
    relation: "",
    phone: "",
    email: ""
  })

  const resetForm = () => {
    setForm({ name: "", relation: "", phone: "", email: "" })
    setEditingId(null)
  }

  const handleAdd = () => {
    if (!form.name) return
    onChange([...contacts, { id: Date.now(), ...form }])
    resetForm()
  }

  const handleEdit = (contact: ContactPerson) => {
    setForm({
      name: contact.name,
      relation: contact.relation,
      phone: contact.phone,
      email: contact.email
    })
    setEditingId(contact.id)
  }

  const handleUpdate = () => {
    if (editingId === null) return
    onChange(contacts.map((contact) => (contact.id === editingId ? { ...contact, ...form } : contact)))
    resetForm()
  }

  const handleDelete = (id: number) => {
    onChange(contacts.filter((contact) => contact.id !== id))
    if (editingId === id) {
      resetForm()
    }
  }

  return (
    <div className="contact-section">
      <h3>Contact Person Details</h3>

      <div className="contact-form">
        <input placeholder="Name" value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} />
        <input placeholder="Relation" value={form.relation} onChange={(event) => setForm({ ...form, relation: event.target.value })} />
        <input placeholder="Phone" value={form.phone} onChange={(event) => setForm({ ...form, phone: event.target.value })} />
        <input placeholder="Email" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} />

        {editingId === null ? (
          <button type="button" className="add-btn" onClick={handleAdd}>
            Add
          </button>
        ) : (
          <>
            <button type="button" className="update-btn" onClick={handleUpdate}>
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
                  <button type="button" className="edit-btn" onClick={() => handleEdit(contact)}>
                    Edit
                  </button>
                  <button type="button" className="delete-btn" onClick={() => handleDelete(contact.id)}>
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
