import { apiDownload, apiFetch } from "../../lib/api/apiClient";

export type JobDescriptionListItem = {
  id: number;
  departmentId: number;
  personName: string;
  workflowStatus: string;
  qualityStatus: string;
  updatedAt: string;
};

export type DepartmentDashboardMetrics = {
  personnelCount: number;
  activePersonnelCount: number;
  archivedPersonnelCount: number;
  healthyDescriptionCount: number;
  incompleteDescriptionCount: number;
  pendingDataCompletionCount: number;
  pendingDepartmentApprovalCount: number;
  underHumanResourcesReviewCount: number;
  approvedDescriptionCount: number;
  rejectedDescriptionCount: number;
  activeProjectCount: number;
  peopleWorkingOnActiveProjectsCount: number;
};

export type ManagedDepartment = {
  id: number;
  name: string;
  isOwnDepartment: boolean;
};

export type PublicSkill = {
  id: number;
  departmentId: number | null;
  name: string;
  ownerDepartmentId: number | null;
  usageDepartmentCount: number;
  canEdit: boolean;
  canDelete: boolean;
};

export type JobDescriptionCatalog = {
  skills: {
    id: number;
    departmentId: number | null;
    name: string;
    ownerDepartmentId: number | null;
    usageDepartmentCount: number;
    canEdit: boolean;
    canDelete: boolean;
  }[];
  tasks: {
    id: number;
    departmentId: number;
    title: string;
    isProject: boolean;
    requiredSkillIds: number[];
  }[];
};

export type CreateJobDescriptionInput = {
  personName: string;
  departmentId: number;
  personnelCode: string;
  education: string;
  fieldOfStudy: string;
  minimumExperience: string;
  skillIds: number[];
  tasks: {
    taskCatalogItemId: number;
    title: string;
    description: string;
    startDate?: string | null;
    endDate?: string | null;
    sortOrder: number;
    weeklyHours?: number | null;
  }[];
};

export type WorkbookImportResult = {
  fileName: string;
  succeeded: boolean;
  versionId: number | null;
  message: string;
  suggestions: string[];
};

export type JobDescriptionDetail = {
  id: number;
  departmentId: number;
  personName: string;
  personnelCode: string | null;
  education: string;
  fieldOfStudy: string;
  minimumExperience: string;
  skillIds: number[];
  tasks: {
    taskCatalogItemId: number;
    title: string;
    description: string;
    startDate: string | null;
    endDate: string | null;
    sortOrder: number;
    weeklyHours: number | null;
  }[];
  unresolvedSkills: {
    rawName: string;
    sortOrder: number;
  }[];
  unresolvedTasks: {
    rawTitle: string;
    description: string;
    startDate: string | null;
    endDate: string | null;
    sortOrder: number;
  }[];
  workflowStatus: string;
  qualityStatus: string;
  rejectionReason: string | null;
};

export type JobDescriptionQualityFinding = {
  code: string;
  message: string;
  actionTarget: string;
  taskCatalogItemId: number | null;
  skillCatalogItemId: number | null;
};

const base = "/api/v1/job-descriptions";

export const jobDescriptionsApi = {
  dashboard: (departmentId?: number) =>
    apiFetch<DepartmentDashboardMetrics>(
      departmentId
        ? `${base}/dashboard?departmentId=${departmentId}`
        : `${base}/dashboard`,
    ),
  managedDepartments: () =>
    apiFetch<ManagedDepartment[]>(`${base}/managed-departments`),
  list: (departmentId?: number) =>
    apiFetch<JobDescriptionListItem[]>(
      departmentId ? `${base}?departmentId=${departmentId}` : base,
    ),
  humanResourcesReview: () =>
    apiFetch<JobDescriptionListItem[]>(`${base}/human-resources-review`),
  humanResourcesCatalog: () =>
    apiFetch<PublicSkill[]>(`${base}/human-resources-catalog`),
  renamePublicSkill: (id: number, name: string) =>
    apiFetch(`${base}/catalog/public-skills/${id}`, {
      method: "PUT",
      body: JSON.stringify({ name }),
    }),
  deactivatePublicSkill: (id: number) =>
    apiFetch(`${base}/catalog/public-skills/${id}`, { method: "DELETE" }),
  detail: (id: number) => apiFetch<JobDescriptionDetail>(`${base}/${id}`),
  analysis: (id: number) =>
    apiFetch<JobDescriptionQualityFinding[]>(`${base}/${id}/analysis`),
  approveByHumanResources: (id: number) =>
    apiFetch(`${base}/${id}/human-resources-approval`, { method: "POST" }),
  rejectByHumanResources: (id: number, reason: string) =>
    apiFetch(`${base}/${id}/human-resources-rejection`, {
      method: "POST",
      body: JSON.stringify({ reason }),
    }),
  catalog: (departmentId?: number) =>
    apiFetch<JobDescriptionCatalog>(
      departmentId
        ? `${base}/catalog?departmentId=${departmentId}`
        : `${base}/catalog`,
    ),
  create: (input: CreateJobDescriptionInput) =>
    apiFetch(`${base}`, { method: "POST", body: JSON.stringify(input) }),
  revise: (id: number, input: CreateJobDescriptionInput) =>
    apiFetch(`${base}/${id}`, { method: "PUT", body: JSON.stringify(input) }),
  createSkill: (departmentId: number, name: string) =>
    apiFetch<{ id: number }>(`${base}/catalog/skills`, {
      method: "POST",
      body: JSON.stringify({ departmentId, name }),
    }),
  createPublicSkill: (ownerDepartmentId: number, name: string) =>
    apiFetch<{ id: number }>(`${base}/catalog/public-skills`, {
      method: "POST",
      body: JSON.stringify({ ownerDepartmentId, name }),
    }),
  createTask: (departmentId: number, title: string, isProject: boolean) =>
    apiFetch<{ id: number }>(`${base}/catalog/tasks`, {
      method: "POST",
      body: JSON.stringify({ departmentId, title, isProject }),
    }),
  setTaskRequiredSkills: (taskId: number, skillIds: number[]) =>
    apiFetch(`${base}/catalog/tasks/${taskId}/required-skills`, {
      method: "PUT",
      body: JSON.stringify({ skillIds }),
    }),
  renameDepartmentSkill: (id: number, name: string) =>
    apiFetch(`${base}/catalog/skills/${id}`, {
      method: "PUT",
      body: JSON.stringify({ name }),
    }),
  deactivateDepartmentSkill: (id: number) =>
    apiFetch(`${base}/catalog/skills/${id}`, { method: "DELETE" }),
  renameDepartmentTask: (id: number, name: string) =>
    apiFetch(`${base}/catalog/tasks/${id}`, {
      method: "PUT",
      body: JSON.stringify({ name }),
    }),
  deactivateDepartmentTask: (id: number) =>
    apiFetch(`${base}/catalog/tasks/${id}`, { method: "DELETE" }),
  import: (files: File[]) => {
    const form = new FormData();
    files.forEach((file) => form.append("files", file));
    return apiFetch<WorkbookImportResult[]>(`${base}/import`, {
      method: "POST",
      body: form,
    });
  },
  download: (id: number) => apiDownload(`${base}/${id}/excel`),
  approveByDepartmentManager: (id: number) =>
    apiFetch(`${base}/${id}/department-approval`, { method: "POST" }),
};
