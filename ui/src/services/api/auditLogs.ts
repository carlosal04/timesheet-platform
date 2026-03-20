import { apiRequest } from '@/services/api/http'

export interface AuditLogItem {
  id: string
  actorUserId: string | null
  sessionId: string | null
  actionType: string
  entityType: string
  entityId: string | null
  result: string
  correlationId: string
  occurredAtUtc: string
}

interface AuditLogsResponse {
  items: AuditLogItem[]
  page: number
  pageSize: number
  totalCount: number
}

export interface AuditLogQuery {
  page: number
  pageSize: number
  actorUserId?: string
  actionType?: string
  entityType?: string
  result?: string
}

export async function listAuditLogs(query: AuditLogQuery) {
  const params = new URLSearchParams()
  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))

  if (query.actorUserId) {
    params.set('actorUserId', query.actorUserId)
  }

  if (query.actionType) {
    params.set('actionType', query.actionType)
  }

  if (query.entityType) {
    params.set('entityType', query.entityType)
  }

  if (query.result) {
    params.set('result', query.result)
  }

  return apiRequest<AuditLogsResponse>(`/audit-logs?${params.toString()}`)
}
