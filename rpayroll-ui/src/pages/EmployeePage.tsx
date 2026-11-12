import { useEffect, useMemo, useState, useRef } from "react"
import type { FormEvent, ReactNode } from "react"
import { apiFetch } from "../lib/api"
import "./EmployeePage.css"

type EmployeePageProps = {
  view: "list" | "create"
  onChangeView: (view: "list" | "create") => void
  token: string
}

type Employee = {
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

type ContactPerson = {
  id: number
  name: string
  relation: string
  phone: string
  email: string
}
function FormField({
  label,
  children
}: {
  label: string
  children: ReactNode
}) {
  return (
    <div className="form-field">
      <label>{label}</label>
      {children}
    </div>
  )
}
export default function EmployeePage({ view, onChangeView, token }: EmployeePageProps) {

  const [employees, setEmployees] = useState<Employee[]>([])
  const [loading, setLoading] = useState(false)
  const [editingEmployee, setEditingEmployee] = useState<Employee | null>(null)
  const editTriggeredRef = useRef(false)
  const [notification, setNotification] = useState<{
    message: string
    type: "success" | "error"
    visible: boolean
  } | null>(null)
  const hideTimerRef = useRef<number | null>(null)
  const clearTimerRef = useRef<number | null>(null)

  const showNotification = (message: string, type: "success" | "error") => {
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

  const loadEmployees = async () => {
    setLoading(true)
    try {
      const response = await apiFetch("/api/employees", {
        headers: {
          Authorization: `Bearer ${token}`
        }
      })
      if (!response.ok) return
      const data = (await response.json()) as Employee[]
      const normalized = Array.isArray(data)
        ? data.map((emp) => ({
            ...emp,
            contactPersons: Array.isArray(emp.contactPersons)
              ? emp.contactPersons.map((contact: any) => ({
                  id: contact.id ?? Date.now(),
                  name: contact.name ?? "",
                  relation: contact.relationship ?? contact.relation ?? "",
                  phone: contact.phone ?? "",
                  email: contact.email ?? ""
                }))
              : []
          }))
        : []
      setEmployees(normalized)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadEmployees()
  }, [token])

  useEffect(() => {
    if (view === "list") {
      setEditingEmployee(null)
      return
    }

    if (view === "create") {
      if (!editTriggeredRef.current) {
        setEditingEmployee(null)
      }
      editTriggeredRef.current = false
    }
  }, [view])

  if (view === "list") {
    return (
      <>
        {notification && (
          <div
            className={`notification ${notification.type} ${
              notification.visible ? "show" : ""
            }`}
          >
            {notification.message}
          </div>
        )}
        <EmployeeList
          employees={employees}
          loading={loading}
          onEdit={(employee) => {
            editTriggeredRef.current = true
            setEditingEmployee(employee)
            onChangeView("create")
          }}
          onDelete={async (id) => {
            const response = await apiFetch(`/api/employees/${id}`, {
              method: "DELETE",
              headers: {
                Authorization: `Bearer ${token}`
              }
            })
            if (response.ok) {
              setEmployees(prev => prev.filter(e => e.id !== id))
              showNotification("Employee deleted", "success")
            } else {
              showNotification("Failed to delete employee", "error")
            }
          }}
        />
      </>
    )
  }

  return (
    <>
      {notification && (
        <div
          className={`notification ${notification.type} ${
            notification.visible ? "show" : ""
          }`}
        >
          {notification.message}
        </div>
      )}
      <CreateEmployee
        employee={editingEmployee}
        token={token}
        onNotify={showNotification}
        onSaved={() => {
          setEditingEmployee(null)
          onChangeView("list")
          loadEmployees()
        }}
        onCancel={() => {
          setEditingEmployee(null)
          onChangeView("list")
        }}
      />
    </>
  )
}

function EmployeeList({
  employees,
  loading,
  onEdit,
  onDelete
}: {
  employees: Employee[]
  loading: boolean
  onEdit: (e: Employee) => void
  onDelete: (id: number) => void
}) {

  const [search, setSearch] = useState("")
  const [sortField, setSortField] = useState<keyof Employee>("firstName")
  const [page] = useState(1)

  const pageSize = 5

  const filteredEmployees = useMemo(() => {
    return employees
      .filter(emp =>
        `${emp.firstName} ${emp.lastName}`
          .toLowerCase()
          .includes(search.toLowerCase())
      )
      .sort((a, b) => {
        const left = String(a[sortField] ?? "")
        const right = String(b[sortField] ?? "")
        return left.localeCompare(right)
      })
  }, [employees, search, sortField])

  const paginatedEmployees = filteredEmployees.slice(
    (page - 1) * pageSize,
    page * pageSize
  )

  return (
    <div className="employee-list">

      <div className="employee-toolbar">

        <input
          placeholder="Search employees..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />

        <select
          onChange={(e) =>
            setSortField(e.target.value as keyof Employee)
          }
        >
          <option value="firstName">Sort by Name</option>
          <option value="department">Department</option>
          <option value="position">Role</option>
        </select>

      </div>

      <table className="employee-table">

        <thead>
          <tr>
            <th>ID</th>
            <th>Name</th>
            <th>Email</th>
            <th>Department</th>
            <th>Role</th>
            <th>Status</th>
            <th>Actions</th>
          </tr>
        </thead>

        <tbody>

          {paginatedEmployees.map(emp => (

            <tr key={emp.id}>

              <td>{emp.id}</td>
              <td>{emp.firstName} {emp.lastName}</td>
              <td>{emp.email ?? "-"}</td>
              <td>{emp.department ?? "-"}</td>
              <td>{emp.position ?? "-"}</td>

              <td>
                <span className="status-active">Active</span>
              </td>

              <td className="employee-actions">

                <button
                  className="edit-btn"
                  onClick={() => onEdit(emp)}
                >
                  Edit
                </button>

                <button
                  className="delete-btn"
                  onClick={() => onDelete(emp.id)}
                >
                  Delete
                </button>

              </td>

            </tr>

          ))}

        </tbody>

      </table>

      {loading && <div className="employee-loading">Loading...</div>}

    </div>
  )
}

function CreateEmployee({
  employee,
  token,
  onNotify,
  onSaved,
  onCancel
}: {
  employee: Employee | null
  token: string
  onNotify: (message: string, type: "success" | "error") => void
  onSaved: () => void
  onCancel: () => void
}) {
  const [activeSection, setActiveSection] = useState("basic")
  const formRef = useRef<HTMLDivElement | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const [form, setForm] = useState({
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

  const [contacts, setContacts] = useState<ContactPerson[]>(
    employee?.contactPersons ?? []
  )

  useEffect(() => {
    setForm({
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
    setContacts(employee?.contactPersons ?? [])
  }, [employee])

  const scrollTo = (id: string) => {
    const el = document.getElementById(id)
    if (el) {
      el.scrollIntoView({
        behavior: "smooth",
        block: "start"
      })
    }
  }

  useEffect(() => {
    const sectionIds = [
      "basic-section",
      "job-section",
      "payroll-section",
      "contact-section"
    ]

    const container = formRef.current
    if (!container) return

    const handleScroll = () => {
      const triggerPoint = container.clientHeight * 0.25
      let currentSection = "basic"

      sectionIds.forEach(id => {
        const el = document.getElementById(id)
        if (!el) return

        const rect = el.getBoundingClientRect()
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

    return () => {
      container.removeEventListener("scroll", handleScroll)
    }
  }, [])

  const handleChange = (
    field: keyof typeof form,
    value: string | boolean
  ) => {
    setForm(prev => ({ ...prev, [field]: value }))
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
      const getErrorMessage = async (response: Response) => {
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

      if (employee) {
        const payload = {
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
          usedSickLeaves: employee.usedSickLeaves ?? 0,
          totalCasualLeaves: toNumber(form.totalCasualLeaves),
          usedCasualLeaves: employee.usedCasualLeaves ?? 0,
          totalGovernmentLeaves: toNumber(form.totalGovernmentLeaves),
          usedGovernmentLeaves: employee.usedGovernmentLeaves ?? 0,
          totalUnpaidLeaves: toNumber(form.totalUnpaidLeaves),
          usedUnpaidLeaves: employee.usedUnpaidLeaves ?? 0
        }

        const response = await apiFetch(`/api/employees/${employee.id}`, {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`
          },
          body: JSON.stringify(payload)
        })

        if (response.ok) {
          onNotify("Employee updated successfully", "success")
          onSaved()
        } else {
          const errorMessage = await getErrorMessage(response)
          onNotify(errorMessage, "error")
        }
      } else {
        const payload = {
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
          totalCasualLeaves: toNumber(form.totalCasualLeaves),
          totalGovernmentLeaves: toNumber(form.totalGovernmentLeaves),
          totalUnpaidLeaves: toNumber(form.totalUnpaidLeaves),
          createLogin: form.createLogin,
          username: form.createLogin ? form.username : null,
          password: form.createLogin ? form.password : null,
          roleName: form.createLogin ? form.roleName : null,
          contactPersons: contacts.map(contact => ({
            id: 0,
            employeeId: 0,
            name: contact.name,
            relationship: contact.relation,
            phone: contact.phone,
            email: contact.email,
            isPrimary: false
          }))
        }

        const response = await apiFetch("/api/employees", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`
          },
          body: JSON.stringify(payload)
        })

        if (response.ok) {
          onNotify("Employee saved successfully", "success")
          onSaved()
        } else {
          const errorMessage = await getErrorMessage(response)
          onNotify(errorMessage, "error")
        }
      }
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
              <input
                value={form.employeeId}
                readOnly
                disabled
              />
            </FormField>

            <FormField label="First Name">
              <input
                value={form.firstName}
                onChange={(e) => handleChange("firstName", e.target.value)}
                required
              />
            </FormField>

            <FormField label="Last Name">
              <input
                value={form.lastName}
                onChange={(e) => handleChange("lastName", e.target.value)}
                required
              />
            </FormField>

            <FormField label="Email">
              <input
                type="email"
                value={form.email}
                onChange={(e) => handleChange("email", e.target.value)}
              />
            </FormField>

            <FormField label="Phone">
              <input
                value={form.phone}
                onChange={(e) => handleChange("phone", e.target.value)}
              />
            </FormField>

            <FormField label="Date of Birth">
              <input
                type="date"
                value={form.dateOfBirth}
                onChange={(e) => handleChange("dateOfBirth", e.target.value)}
              />
            </FormField>

            <FormField label="Address">
              <input
                value={form.address}
                onChange={(e) => handleChange("address", e.target.value)}
              />
            </FormField>
          </div>

          <div id="job-section" className="form-section">
            <h3>Job Details</h3>

            <FormField label="Department">
              <input
                value={form.department}
                onChange={(e) => handleChange("department", e.target.value)}
              />
            </FormField>

            <FormField label="Role">
              <input
                value={form.position}
                onChange={(e) => handleChange("position", e.target.value)}
              />
            </FormField>

            <FormField label="Manager ID">
              <input
                value={form.managerId}
                onChange={(e) => handleChange("managerId", e.target.value)}
              />
            </FormField>

            <FormField label="Date of Joining">
              <input
                type="date"
                value={form.dateOfJoining}
                onChange={(e) => handleChange("dateOfJoining", e.target.value)}
              />
            </FormField>
          </div>

          <div id="payroll-section" className="form-section">
            <h3>Payroll Details</h3>

            <FormField label="Salary">
              <input
                type="number"
                value={form.basicSalary}
                onChange={(e) => handleChange("basicSalary", e.target.value)}
                min="0"
              />
            </FormField>

            <FormField label="Bank Name">
              <input
                value={form.bankName}
                onChange={(e) => handleChange("bankName", e.target.value)}
              />
            </FormField>

            <FormField label="Bank Account">
              <input
                value={form.bankAccountNumber}
                onChange={(e) => handleChange("bankAccountNumber", e.target.value)}
              />
            </FormField>

            <FormField label="IFSC">
              <input
                value={form.ifscCode}
                onChange={(e) => handleChange("ifscCode", e.target.value)}
              />
            </FormField>

            <FormField label="Total Sick Leaves">
              <input
                type="number"
                min="0"
                value={form.totalSickLeaves}
                onChange={(e) => handleChange("totalSickLeaves", e.target.value)}
              />
            </FormField>

            <FormField label="Total Casual Leaves">
              <input
                type="number"
                min="0"
                value={form.totalCasualLeaves}
                onChange={(e) => handleChange("totalCasualLeaves", e.target.value)}
              />
            </FormField>

            <FormField label="Total Government Leaves">
              <input
                type="number"
                min="0"
                value={form.totalGovernmentLeaves}
                onChange={(e) => handleChange("totalGovernmentLeaves", e.target.value)}
              />
            </FormField>

            <FormField label="Total Unpaid Leaves">
              <input
                type="number"
                min="0"
                value={form.totalUnpaidLeaves}
                onChange={(e) => handleChange("totalUnpaidLeaves", e.target.value)}
              />
            </FormField>

            <FormField label="Create Login">
              <input
                type="checkbox"
                checked={form.createLogin}
                onChange={(e) => handleChange("createLogin", e.target.checked)}
              />
            </FormField>

            {form.createLogin && (
              <>
                <FormField label="Login ID">
                  <input
                    value={form.username}
                    onChange={(e) => handleChange("username", e.target.value)}
                    required
                  />
                </FormField>

                <FormField label="Login Password">
                  <input
                    type="password"
                    value={form.password}
                    onChange={(e) => handleChange("password", e.target.value)}
                    required
                  />
                </FormField>

                <FormField label="Role">
                  <input
                    value={form.roleName}
                    onChange={(e) => handleChange("roleName", e.target.value)}
                  />
                </FormField>
              </>
            )}
          </div>

          <div id="contact-section" className="form-section">
            <ContactPersonSection
              contacts={contacts}
              onChange={setContacts}
            />
          </div>

          <div className="form-actions">
            <button
              type="submit"
              className="primary-btn-1"
              disabled={submitting}
            >
              {employee ? "Edit Employee" : "Save Employee"}
            </button>
            <button
              type="button"
              className="cancel-btn"
              onClick={onCancel}
            >
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
    setForm({
      name: "",
      relation: "",
      phone: "",
      email: ""
    })
    setEditingId(null)
  }

  const handleAdd = () => {

    if (!form.name) return

    onChange([
      ...contacts,
      { id: Date.now(), ...form }
    ])

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
    onChange(
      contacts.map(contact =>
        contact.id === editingId ? { ...contact, ...form } : contact
      )
    )
    resetForm()
  }

  const handleDelete = (id: number) => {
    onChange(contacts.filter(c => c.id !== id))
    if (editingId === id) {
      resetForm()
    }
  }

  return (
    <div className="contact-section">

      <h3>Contact Person Details</h3>

      <div className="contact-form">

        <input
          placeholder="Name"
          value={form.name}
          onChange={(e) =>
            setForm({ ...form, name: e.target.value })
          }
        />

        <input
          placeholder="Relation"
          value={form.relation}
          onChange={(e) =>
            setForm({ ...form, relation: e.target.value })
          }
        />

        <input
          placeholder="Phone"
          value={form.phone}
          onChange={(e) =>
            setForm({ ...form, phone: e.target.value })
          }
        />

        <input
          placeholder="Email"
          value={form.email}
          onChange={(e) =>
            setForm({ ...form, email: e.target.value })
          }
        />

        {editingId === null ? (
          <button
            type="button"
            className="add-btn"
            onClick={handleAdd}
          >
            Add
          </button>
        ) : (
          <>
            <button
              type="button"
              className="update-btn"
              onClick={handleUpdate}
            >
              Update
            </button>
            <button
              type="button"
              className="cancel-btn"
              onClick={resetForm}
            >
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
            {contacts.map(c => (
              <tr key={c.id}>
                <td>{c.name}</td>
                <td>{c.relation}</td>
                <td>{c.phone}</td>
                <td>{c.email}</td>
                <td className="employee-actions">
                  <button
                    type="button"
                    className="edit-btn"
                    onClick={() => handleEdit(c)}
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    className="delete-btn"
                    onClick={() => handleDelete(c.id)}
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
