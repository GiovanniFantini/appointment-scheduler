// Auth
export { AuthProvider, useAuth, useOptionalAuth } from './auth/AuthContext'
export type { BaseUser, AuthContextValue } from './auth/types'

// Shell
export { AppShell } from './shell/AppShell'
export type { AppShellProps, AppShellFab, NavItem, NavSection } from './shell/AppShell'
export { BottomNav, Fab } from './shell/BottomNav'
export { Sidebar } from './shell/Sidebar'
export { TopHeader } from './shell/TopHeader'
export { UserMenu } from './shell/UserMenu'
export type { UserMenuItem } from './shell/UserMenu'
export { PageHeader } from './shell/PageHeader'
export { Breadcrumb } from './shell/Breadcrumb'
export type { BreadcrumbItem } from './shell/Breadcrumb'

// UI primitives
export { Button } from './ui/Button'
export type { ButtonProps, ButtonVariant, ButtonSize } from './ui/Button'
export { IconButton } from './ui/IconButton'
export { Modal } from './ui/Modal'
export type { ModalProps, ModalSize } from './ui/Modal'
export { ConfirmDialog } from './ui/ConfirmDialog'
export { ConfirmProvider, useConfirm } from './ui/ConfirmProvider'
export { Card } from './ui/Card'
export { Badge } from './ui/Badge'
export type { BadgeVariant } from './ui/Badge'
export { Skeleton } from './ui/Skeleton'
export { EmptyState } from './ui/EmptyState'
export { BottomSheet } from './ui/BottomSheet'
export type { BottomSheetProps } from './ui/BottomSheet'
export { Avatar } from './ui/Avatar'
export type { AvatarProps, AvatarSize } from './ui/Avatar'
export { AvatarStack } from './ui/AvatarStack'
export type { AvatarStackProps, AvatarStackPerson } from './ui/AvatarStack'
export { StatusChip } from './ui/StatusChip'
export type { StatusChipProps, StatusChipVariant } from './ui/StatusChip'
export { SegmentedTabs } from './ui/SegmentedTabs'
export type { SegmentedTabsProps, SegmentedTabOption } from './ui/SegmentedTabs'
export { KeyValueRows } from './ui/KeyValueRows'
export type { KeyValueRowsProps, KeyValueRow } from './ui/KeyValueRows'

// Toast
export { Toaster, useToast } from './ui/Toast/Toaster'
export type { ToastVariant } from './ui/Toast/Toaster'

// Form
export { FormField } from './form/FormField'
export { Input } from './form/Input'
export { Select } from './form/Select'
export { Textarea } from './form/Textarea'
export { Checkbox } from './form/Checkbox'
export { DatePicker } from './form/DatePicker'
export { useFormValidation } from './form/useFormValidation'
export type { ValidationRule, FieldErrors } from './form/useFormValidation'

// Self-service pages (Profilo / Preferenze)
export { ProfilePage } from './pages/ProfilePage'
export { PreferencesPage, getStoredTheme, applyTheme, initTheme } from './pages/PreferencesPage'

// Wizard
export { Wizard } from './wizard/Wizard'
export type { WizardProps, WizardStepConfig } from './wizard/Wizard'
export { StepIndicator } from './wizard/StepIndicator'
export { useWizard } from './wizard/useWizard'

// Hooks
export { useMediaQuery } from './hooks/useMediaQuery'
export { useIsMobile } from './hooks/useIsMobile'

// Icons re-export di comodo (singolo punto di import)

// Icons (set minimo riusabile da tutte le app)
export * from './icons'

// Icone Tabler (set del mockup Turnis)
export * from './icons/tabler'
