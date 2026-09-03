import { apiFetch } from "../../lib/api/apiClient";

export type PagedResult<T> = {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
};

export type ManagedUser = {
  id: number;
  personnelCode: string;
  firstName: string;
  lastName: string;
  username: string | null;
  maskedMobile: string;
  departmentId: number;
  departmentName: string | null;
  isActive: boolean;
  mustChangePassword: boolean;
  roleIds: number[];
  rowVersion: string;
};

export type ManagedDepartment = {
  id: number;
  name: string;
  parentDepartmentId: number | null;
  rowVersion: string;
};

export type ManagedRole = { id: number; code: string; displayName: string };

export type AuditLog = {
  id: number;
  occurredAt: string;
  eventCode: string;
  succeeded: boolean;
  actorUserId: number | null;
  actorDisplayName: string | null;
  subjectUserId: number | null;
  subjectDisplayName: string | null;
  clientIpAddress: string | null;
  clientDeviceKind: string | null;
};

export type AdministrationDashboard = {
  activeUsers: number;
  inactiveUsers: number;
  successfulSignIns: number;
  failedSecurityAttempts: number;
  usersWithActiveSessions: number;
  latestAuditLogs: AuditLog[];
};

export type UserInput = {
  personnelCode: string;
  firstName: string;
  lastName: string;
  replacementMobile?: string | null;
  username?: string;
  departmentId: number;
  roleIds: number[];
};

const base = "/api/v1/administration";

export const administrationApi = {
  dashboard: () => apiFetch<AdministrationDashboard>(`${base}/dashboard`),
  users: (pageNumber = 1, pageSize = 25) =>
    apiFetch<PagedResult<ManagedUser>>(
      `${base}/users?pageNumber=${pageNumber}&pageSize=${pageSize}`,
    ),
  user: (id: number) => apiFetch<ManagedUser>(`${base}/users/${id}`),
  roles: () => apiFetch<ManagedRole[]>(`${base}/roles`),
  departments: () => apiFetch<ManagedDepartment[]>(`${base}/departments`),
  auditLogs: (query: URLSearchParams) =>
    apiFetch<PagedResult<AuditLog>>(`${base}/audit-logs?${query.toString()}`),
  createUser: (
    input: UserInput & { mobile: string; temporaryPassword: string },
  ) =>
    apiFetch<ManagedUser>(`${base}/users`, {
      method: "POST",
      body: JSON.stringify(input),
    }),
  updateUser: (id: number, input: UserInput & { rowVersion: string }) =>
    apiFetch<ManagedUser>(`${base}/users/${id}`, {
      method: "PUT",
      body: JSON.stringify(input),
    }),
  setUserActive: (id: number, isActive: boolean, rowVersion: string) =>
    apiFetch<ManagedUser>(`${base}/users/${id}/active`, {
      method: "PUT",
      body: JSON.stringify({ isActive, rowVersion }),
    }),
  resetPassword: (id: number, temporaryPassword: string, rowVersion: string) =>
    apiFetch<ManagedUser>(`${base}/users/${id}/password-reset`, {
      method: "POST",
      body: JSON.stringify({ temporaryPassword, rowVersion }),
    }),
  createDepartment: (name: string, parentDepartmentId: number | null) =>
    apiFetch<ManagedDepartment>(`${base}/departments`, {
      method: "POST",
      body: JSON.stringify({ name, parentDepartmentId }),
    }),
  updateDepartment: (
    id: number,
    name: string,
    parentDepartmentId: number | null,
    rowVersion: string,
  ) =>
    apiFetch<ManagedDepartment>(`${base}/departments/${id}`, {
      method: "PUT",
      body: JSON.stringify({ name, parentDepartmentId, rowVersion }),
    }),
  deleteDepartment: (id: number, rowVersion: string) =>
    apiFetch<void>(`${base}/departments/${id}`, {
      method: "DELETE",
      body: JSON.stringify({ rowVersion }),
    }),
};
