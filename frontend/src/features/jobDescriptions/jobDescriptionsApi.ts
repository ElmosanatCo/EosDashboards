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

export type HumanResourcesDepartment = {
  id: number;
  name: string;
};

export type HumanResourcesChangeSummary = {
  departmentId: number;
  departmentName: string;
  changeCount: number;
  latestChangedAt: string | null;
};

export type HumanResourcesChange = {
  versionId: number;
  departmentId: number;
  departmentName: string;
  personName: string;
  changeType: string;
  changedAt: string;
  actorUserId: number | null;
};

export type HumanResourcesDashboard = {
  metrics: DepartmentDashboardMetrics;
  changeSummaries: HumanResourcesChangeSummary[];
  changes: HumanResourcesChange[];
  totalChangeCount: number;
  page: number;
  pageSize: number;
};

export type JobDescriptionComparisonTask = {
  taskCatalogItemId: number;
  title: string;
  description: string;
  startDate: string | null;
  endDate: string | null;
  sortOrder: number;
  weeklyHours: number | null;
};

export type JobDescriptionComparisonSnapshot = {
  versionId: number;
  personName: string;
  departmentId: number;
  personnelCode: string | null;
  education: string;
  fieldOfStudy: string;
  minimumExperience: string;
  skillIds: number[];
  tasks: JobDescriptionComparisonTask[];
  workflowStatus: string;
  qualityStatus: string;
  updatedAt: string;
};

export type JobDescriptionComparison = {
  currentVersionId: number;
  previousVersionId: number | null;
  current: JobDescriptionComparisonSnapshot;
  previous: JobDescriptionComparisonSnapshot | null;
  changes: {
    field: string;
    kind: string;
    before: string | null;
    after: string | null;
  }[];
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
  isActive: boolean;
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
    isActive: boolean;
    canEdit: boolean;
    canDelete: boolean;
  }[];
  tasks: {
    id: number;
    departmentId: number;
    title: string;
    isProject: boolean;
    isActive: boolean;
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
  humanResourcesDepartments: () =>
    apiFetch<HumanResourcesDepartment[]>(`${base}/human-resources-departments`),
  humanResourcesDashboard: (departmentId?: number, page = 1, pageSize = 20) => {
    const query = new URLSearchParams({
      page: String(page),
      pageSize: String(pageSize),
    });
    if (departmentId) query.set("departmentId", String(departmentId));
    return apiFetch<HumanResourcesDashboard>(
      `${base}/human-resources-dashboard?${query}`,
    );
  },
  humanResourcesReview: (departmentId?: number) =>
    apiFetch<JobDescriptionListItem[]>(
      departmentId
        ? `${base}/human-resources-review?departmentId=${departmentId}`
        : `${base}/human-resources-review`,
    ),
  humanResourcesApproved: (departmentId?: number) =>
    apiFetch<JobDescriptionListItem[]>(
      departmentId
        ? `${base}/human-resources-approved?departmentId=${departmentId}`
        : `${base}/human-resources-approved`,
    ),
  humanResourcesCatalog: (includeInactive = false) =>
    apiFetch<PublicSkill[]>(
      `${base}/human-resources-catalog?includeInactive=${includeInactive}`,
    ),
  renamePublicSkill: (id: number, name: string) =>
    apiFetch(`${base}/catalog/public-skills/${id}`, {
      method: "PUT",
      body: JSON.stringify({ name }),
    }),
  deactivatePublicSkill: (id: number) =>
    apiFetch(`${base}/catalog/public-skills/${id}`, { method: "DELETE" }),
  activatePublicSkill: (id: number) =>
    apiFetch(`${base}/catalog/public-skills/${id}/active`, { method: "PUT" }),
  mergePublicSkill: (sourceId: number, survivingSkillId: number) =>
    apiFetch(`${base}/catalog/public-skills/${sourceId}/merge`, {
      method: "POST",
      body: JSON.stringify({ survivingSkillId }),
    }),
  detail: (id: number) => apiFetch<JobDescriptionDetail>(`${base}/${id}`),
  comparison: (id: number) =>
    apiFetch<JobDescriptionComparison>(`${base}/${id}/comparison`),
  analysis: (id: number) =>
    apiFetch<JobDescriptionQualityFinding[]>(`${base}/${id}/analysis`),
  approveByHumanResources: (id: number) =>
    apiFetch(`${base}/${id}/human-resources-approval`, { method: "POST" }),
  rejectByHumanResources: (id: number, reason: string) =>
    apiFetch(`${base}/${id}/human-resources-rejection`, {
      method: "POST",
      body: JSON.stringify({ reason }),
    }),
  catalog: (departmentId?: number, includeInactive = false) =>
    apiFetch<JobDescriptionCatalog>(
      departmentId
        ? `${base}/catalog?departmentId=${departmentId}&includeInactive=${includeInactive}`
        : `${base}/catalog?includeInactive=${includeInactive}`,
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
  activateDepartmentSkill: (id: number) =>
    apiFetch(`${base}/catalog/skills/${id}/active`, { method: "PUT" }),
  renameDepartmentTask: (id: number, name: string) =>
    apiFetch(`${base}/catalog/tasks/${id}`, {
      method: "PUT",
      body: JSON.stringify({ name }),
    }),
  deactivateDepartmentTask: (id: number) =>
    apiFetch(`${base}/catalog/tasks/${id}`, { method: "DELETE" }),
  activateDepartmentTask: (id: number) =>
    apiFetch(`${base}/catalog/tasks/${id}/active`, { method: "PUT" }),
  import: (files: File[]) => {
    const form = new FormData();
    files.forEach((file) => form.append("files", file));
    return apiFetch<WorkbookImportResult[]>(`${base}/import`, {
      method: "POST",
      body: form,
    });
  },
  download: (id: number) => apiDownload(`${base}/${id}/excel`),
  archive: (id: number) =>
    apiFetch(`${base}/${id}/archive`, { method: "POST" }),
  delete: (id: number) => apiFetch(`${base}/${id}`, { method: "DELETE" }),
  approveByDepartmentManager: (id: number) =>
    apiFetch(`${base}/${id}/department-approval`, { method: "POST" }),
};
