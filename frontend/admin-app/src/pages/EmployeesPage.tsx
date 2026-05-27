import { PageHeader } from '@scheduler/ui'
import EmployeesTable from './EmployeesTable'

export default function EmployeesPage() {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
      <PageHeader
        title="Employees"
        subtitle="Tutti gli employee della piattaforma, anche quelli pre-caricati senza account"
      />
      <EmployeesTable />
    </div>
  )
}
