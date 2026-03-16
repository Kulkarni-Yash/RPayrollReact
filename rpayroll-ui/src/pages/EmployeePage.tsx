import { useEffect, useRef, useState } from "react"
import EntityList from "../components/listing/EntityList"
import EmployeeForm, { type Employee } from "../components/employee/EmployeeForm"
import { apiFetch } from "../lib/api"
import "./EmployeePage.css"

type EmployeePageProps = {
  view: "list" | "create"
  onChangeView: (view: "list" | "create") => void
  token: string
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
      setNotification((current) => (current ? { ...current, visible: false } : current))
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
        ? data.map((employee) => ({
            ...employee,
            contactPersons: Array.isArray(employee.contactPersons)
              ? employee.contactPersons.map((contact: any) => ({
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

  const handleDelete = async (id: number) => {
    const response = await apiFetch(`/api/employees/${id}`, {
      method: "DELETE",
      headers: {
        Authorization: `Bearer ${token}`
      }
    })

    if (response.ok) {
      setEmployees((current) => current.filter((employee) => employee.id !== id))
      showNotification("Employee deleted", "success")
      return
    }

    showNotification("Failed to delete employee", "error")
  }

  const columns = [
    {
      key: "id",
      header: "ID",
      sortable: true,
      sortValue: (employee: Employee) => employee.id,
      render: (employee: Employee) => employee.id
    },
    {
      key: "name",
      header: "Name",
      sortable: true,
      sortValue: (employee: Employee) => `${employee.firstName} ${employee.lastName}`,
      render: (employee: Employee) => `${employee.firstName} ${employee.lastName}`
    },
    {
      key: "email",
      header: "Email",
      sortable: true,
      sortValue: (employee: Employee) => employee.email ?? "",
      render: (employee: Employee) => employee.email ?? "-"
    },
    {
      key: "department",
      header: "Department",
      sortable: true,
      sortValue: (employee: Employee) => employee.department ?? "",
      render: (employee: Employee) => employee.department ?? "-"
    },
    {
      key: "role",
      header: "Role",
      sortable: true,
      sortValue: (employee: Employee) => employee.position ?? "",
      render: (employee: Employee) => employee.position ?? "-"
    },
    {
      key: "status",
      header: "Status",
      render: () => <span className="status-active">Active</span>
    },
    {
      key: "actions",
      header: "Actions",
      render: (employee: Employee) => (
        <div className="employee-actions">
          <button
            className="edit-btn"
            onClick={() => {
              editTriggeredRef.current = true
              setEditingEmployee(employee)
              onChangeView("create")
            }}
          >
            Edit
          </button>
          <button className="delete-btn" onClick={() => handleDelete(employee.id)}>
            Delete
          </button>
        </div>
      )
    }
  ]

  return (
    <>
      {notification && (
        <div className={`notification ${notification.type} ${notification.visible ? "show" : ""}`}>
          {notification.message}
        </div>
      )}

      {view === "list" ? (
        <EntityList
          title="Employees"
          items={employees}
          columns={columns}
          loading={loading}
          rowKey={(employee) => employee.id}
          searchPlaceholder="Search employees..."
          searchValue={(employee) =>
            `${employee.firstName} ${employee.lastName} ${employee.email ?? ""} ${employee.department ?? ""}`
          }
          emptyMessage="No employees found"
        />
      ) : (
        <EmployeeForm
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
      )}
    </>
  )
}
