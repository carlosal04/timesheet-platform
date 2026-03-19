export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  [key: string]: unknown
}

export class ApiProblemError extends Error {
  status: number
  problem: ProblemDetails

  constructor(problem: ProblemDetails, fallbackMessage = 'Request failed') {
    super(problem.title ?? fallbackMessage)
    this.name = 'ApiProblemError'
    this.status = problem.status ?? 0
    this.problem = problem
  }
}

interface AntiforgeryToken {
  headerName: string
  requestToken: string
}

interface ApiRequestOptions {
  method?: string
  body?: BodyInit | Record<string, unknown> | null
  headers?: HeadersInit
  signal?: AbortSignal
  skipAntiforgery?: boolean
  retryOnAntiforgeryFailure?: boolean
}

const API_BASE_PATH = '/api'
const SAFE_METHODS = new Set(['GET', 'HEAD', 'OPTIONS', 'TRACE'])

let antiforgeryToken: AntiforgeryToken | null = null
let antiforgeryPromise: Promise<AntiforgeryToken> | null = null

export function clearAntiforgeryToken() {
  antiforgeryToken = null
}

export function isApiProblemError(error: unknown): error is ApiProblemError {
  return error instanceof ApiProblemError
}

export function isProblemStatus(error: unknown, status: number) {
  return isApiProblemError(error) && error.status === status
}

function buildUrl(path: string) {
  return `${API_BASE_PATH}${path.startsWith('/') ? path : `/${path}`}`
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null && ('title' in value || 'status' in value || 'detail' in value)
}

function isAntiforgeryProblem(problem: ProblemDetails) {
  return problem.status === 400 && problem.title === 'Invalid anti-forgery token'
}

async function readJson(response: Response) {
  const text = await response.text()
  if (!text) {
    return null
  }

  try {
    return JSON.parse(text) as unknown
  } catch {
    return null
  }
}

async function readProblem(response: Response) {
  const payload = await readJson(response)
  if (isProblemDetails(payload)) {
    return {
      ...payload,
      status: payload.status ?? response.status,
    }
  }

  return {
    status: response.status,
    title: response.statusText || 'Request failed',
  } satisfies ProblemDetails
}

async function readData<T>(response: Response): Promise<T> {
  if (response.status === 204) {
    return undefined as T
  }

  const payload = await readJson(response)
  return payload as T
}

function toBodyAndHeaders(body: ApiRequestOptions['body'], headers: Headers) {
  if (body === undefined) {
    return undefined
  }

  if (
    body === null ||
    typeof body === 'string' ||
    body instanceof Blob ||
    body instanceof FormData ||
    body instanceof URLSearchParams ||
    body instanceof ArrayBuffer
  ) {
    return body
  }

  headers.set('Content-Type', 'application/json')
  return JSON.stringify(body)
}

export async function ensureAntiforgeryToken(forceRefresh = false): Promise<AntiforgeryToken> {
  if (!forceRefresh && antiforgeryToken) {
    return antiforgeryToken
  }

  if (!forceRefresh && antiforgeryPromise) {
    return antiforgeryPromise
  }

  antiforgeryPromise = (async () => {
    const response = await fetch(buildUrl('/auth/antiforgery'), {
      method: 'GET',
      credentials: 'include',
      headers: {
        Accept: 'application/json',
      },
    })

    if (!response.ok) {
      const problem = await readProblem(response)
      throw new ApiProblemError(problem, 'Unable to bootstrap anti-forgery protection')
    }

    const token = (await readData<AntiforgeryToken>(response)) satisfies AntiforgeryToken
    antiforgeryToken = token
    return token
  })()

  try {
    return await antiforgeryPromise
  } finally {
    antiforgeryPromise = null
  }
}

export async function apiRequest<T>(path: string, options: ApiRequestOptions = {}): Promise<T> {
  const method = (options.method ?? 'GET').toUpperCase()
  const shouldSendAntiforgery = !options.skipAntiforgery && !SAFE_METHODS.has(method)
  const headers = new Headers(options.headers)

  headers.set('Accept', 'application/json')

  if (shouldSendAntiforgery) {
    const token = await ensureAntiforgeryToken()
    headers.set(token.headerName, token.requestToken)
  }

  const response = await fetch(buildUrl(path), {
    method,
    credentials: 'include',
    headers,
    body: toBodyAndHeaders(options.body, headers),
    signal: options.signal,
  })

  if (!response.ok) {
    const problem = await readProblem(response)
    if (
      shouldSendAntiforgery &&
      options.retryOnAntiforgeryFailure !== false &&
      isAntiforgeryProblem(problem)
    ) {
      clearAntiforgeryToken()
      await ensureAntiforgeryToken(true)
      return apiRequest<T>(path, {
        ...options,
        retryOnAntiforgeryFailure: false,
      })
    }

    throw new ApiProblemError(problem)
  }

  return readData<T>(response)
}
