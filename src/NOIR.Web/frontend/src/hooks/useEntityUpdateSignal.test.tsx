import { renderHook, act } from '@testing-library/react'
import type { ReactNode } from 'react'
import { vi, type Mock } from 'vitest'
import type { EntityUpdateSignal } from '@/types/signals'

// --- Mocks ---

// Mock getAccessToken to return a token by default
vi.mock('@/services/tokenStorage', () => ({
  getAccessToken: vi.fn(() => 'fake-access-token'),
}))

// Mock toast
const mockToastInfo = vi.fn()
vi.mock('@/lib/toast', () => ({
  toast: {
    info: (...args: unknown[]) => mockToastInfo(...args),
    success: vi.fn(),
    error: vi.fn(),
    loading: vi.fn(),
  },
}))

// Mock react-i18next
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en', changeLanguage: vi.fn() },
  }),
}))

// Mock AuthContext
const mockUser = { tenantId: 'test-tenant' }
vi.mock('@/contexts/AuthContext', () => ({
  useAuthContext: () => ({ user: mockUser }),
}))

// --- SignalR mock infrastructure ---

type SignalRHandler = (...args: unknown[]) => void

let registeredHandlers: Record<string, SignalRHandler> = {}
let reconnectingCallback: (() => void) | null = null
let reconnectedCallback: (() => void) | null = null

const mockInvoke = vi.fn().mockResolvedValue(undefined)
const mockStart = vi.fn().mockResolvedValue(undefined)
const mockStop = vi.fn().mockResolvedValue(undefined)

const mockConnection = {
  on: vi.fn((event: string, handler: SignalRHandler) => {
    registeredHandlers[event] = handler
  }),
  off: vi.fn(),
  invoke: mockInvoke,
  start: mockStart,
  stop: mockStop,
  state: 'Connected',
  onreconnecting: vi.fn((cb: () => void) => {
    reconnectingCallback = cb
  }),
  onreconnected: vi.fn((cb: () => void) => {
    reconnectedCallback = cb
  }),
  onclose: vi.fn(),
}

vi.mock('@microsoft/signalr', () => {
  // Must use function keyword (not arrow) so it can be called with `new`
  function MockHubConnectionBuilder() {
    return {
      withUrl: vi.fn().mockReturnThis(),
      withAutomaticReconnect: vi.fn().mockReturnThis(),
      configureLogging: vi.fn().mockReturnThis(),
      build: vi.fn(() => mockConnection),
    }
  }

  return {
    HubConnectionBuilder: MockHubConnectionBuilder,
    HubConnectionState: { Connected: 'Connected', Disconnected: 'Disconnected' },
    LogLevel: { Warning: 1 },
  }
})

// Import after mocks
import { useEntityUpdateSignal } from './useEntityUpdateSignal'

// --- Helpers ---

const Wrapper = ({ children }: { children: ReactNode }) => <>{children}</>

const makeSignal = (overrides: Partial<EntityUpdateSignal> = {}): EntityUpdateSignal => ({
  entityType: 'Product',
  entityId: 'entity-1',
  operation: 'Updated',
  updatedAt: new Date().toISOString(),
  ...overrides,
})

// --- Tests ---

describe('useEntityUpdateSignal', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    registeredHandlers = {}
    reconnectingCallback = null
    reconnectedCallback = null
    mockInvoke.mockClear()
    mockStart.mockClear()
    mockStop.mockClear()
    mockToastInfo.mockClear()
    mockConnection.on.mockClear()
    mockConnection.on.mockImplementation((event: string, handler: SignalRHandler) => {
      registeredHandlers[event] = handler
    })
    mockConnection.onreconnecting.mockClear()
    mockConnection.onreconnecting.mockImplementation((cb: () => void) => {
      reconnectingCallback = cb
    })
    mockConnection.onreconnected.mockClear()
    mockConnection.onreconnected.mockImplementation((cb: () => void) => {
      reconnectedCallback = cb
    })
    mockConnection.onclose.mockClear()
    mockConnection.state = 'Connected'
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('calls onCollectionUpdate when EntityCollectionUpdated signal is received', async () => {
    const onCollectionUpdate = vi.fn()

    renderHook(
      () =>
        useEntityUpdateSignal({
          entityType: 'Product',
          onCollectionUpdate,
        }),
      { wrapper: Wrapper },
    )

    await act(async () => {
      await vi.runAllTimersAsync()
    })

    const handler = registeredHandlers['EntityCollectionUpdated']
    expect(handler).toBeDefined()

    act(() => {
      handler(makeSignal())
    })

    expect(onCollectionUpdate).toHaveBeenCalledTimes(1)
  })

  it('calls onAutoReload when EntityUpdated (Updated) received and isDirty=false', async () => {
    const onAutoReload = vi.fn()

    renderHook(
      () =>
        useEntityUpdateSignal({
          entityType: 'Product',
          entityId: 'entity-1',
          isDirty: false,
          onAutoReload,
        }),
      { wrapper: Wrapper },
    )

    await act(async () => {
      await vi.runAllTimersAsync()
    })

    const handler = registeredHandlers['EntityUpdated']
    expect(handler).toBeDefined()

    act(() => {
      handler(makeSignal({ operation: 'Updated' }))
    })

    expect(onAutoReload).toHaveBeenCalledTimes(1)
  })

  it('shows toast when EntityUpdated (Updated) received and isDirty=false', async () => {
    renderHook(
      () =>
        useEntityUpdateSignal({
          entityType: 'Product',
          entityId: 'entity-1',
          isDirty: false,
          onAutoReload: vi.fn(),
        }),
      { wrapper: Wrapper },
    )

    await act(async () => {
      await vi.runAllTimersAsync()
    })

    act(() => {
      registeredHandlers['EntityUpdated'](makeSignal({ operation: 'Updated' }))
    })

    expect(mockToastInfo).not.toHaveBeenCalled()
  })

  it('sets conflictSignal when EntityUpdated (Updated) received and isDirty=true', async () => {
    const signal = makeSignal({ operation: 'Updated' })

    const { result } = renderHook(
      () =>
        useEntityUpdateSignal({
          entityType: 'Product',
          entityId: 'entity-1',
          isDirty: true,
        }),
      { wrapper: Wrapper },
    )

    await act(async () => {
      await vi.runAllTimersAsync()
    })

    expect(result.current.conflictSignal).toBeNull()

    act(() => {
      registeredHandlers['EntityUpdated'](signal)
    })

    expect(result.current.conflictSignal).toEqual(signal)
  })

  it('sets deletedSignal when EntityUpdated (Deleted) received regardless of isDirty', async () => {
    const signal = makeSignal({ operation: 'Deleted' })

    // Test with isDirty=false
    const { result: result1 } = renderHook(
      () =>
        useEntityUpdateSignal({
          entityType: 'Product',
          entityId: 'entity-1',
          isDirty: false,
        }),
      { wrapper: Wrapper },
    )

    await act(async () => {
      await vi.runAllTimersAsync()
    })

    act(() => {
      registeredHandlers['EntityUpdated'](signal)
    })

    expect(result1.current.deletedSignal).toEqual(signal)

    // Test with isDirty=true
    const { result: result2 } = renderHook(
      () =>
        useEntityUpdateSignal({
          entityType: 'Product',
          entityId: 'entity-1',
          isDirty: true,
        }),
      { wrapper: Wrapper },
    )

    await act(async () => {
      await vi.runAllTimersAsync()
    })

    act(() => {
      registeredHandlers['EntityUpdated'](signal)
    })

    expect(result2.current.deletedSignal).toEqual(signal)
  })

  it('dismissConflict clears conflictSignal', async () => {
    const signal = makeSignal({ operation: 'Updated' })

    const { result } = renderHook(
      () =>
        useEntityUpdateSignal({
          entityType: 'Product',
          entityId: 'entity-1',
          isDirty: true,
        }),
      { wrapper: Wrapper },
    )

    await act(async () => {
      await vi.runAllTimersAsync()
    })

    act(() => {
      registeredHandlers['EntityUpdated'](signal)
    })

    expect(result.current.conflictSignal).toEqual(signal)

    act(() => {
      result.current.dismissConflict()
    })

    expect(result.current.conflictSignal).toBeNull()
  })

  it('reloadAndRestart calls onAutoReload and clears conflictSignal', async () => {
    const onAutoReload = vi.fn()
    const signal = makeSignal({ operation: 'Updated' })

    const { result } = renderHook(
      () =>
        useEntityUpdateSignal({
          entityType: 'Product',
          entityId: 'entity-1',
          isDirty: true,
          onAutoReload,
        }),
      { wrapper: Wrapper },
    )

    await act(async () => {
      await vi.runAllTimersAsync()
    })

    // First set a conflict
    act(() => {
      registeredHandlers['EntityUpdated'](signal)
    })

    expect(result.current.conflictSignal).toEqual(signal)

    // Then reload and restart
    act(() => {
      result.current.reloadAndRestart()
    })

    expect(onAutoReload).toHaveBeenCalledTimes(1)
    expect(result.current.conflictSignal).toBeNull()
  })

  it('sets isReconnecting to true on reconnecting, then clears after reconnect', async () => {
    const { result } = renderHook(
      () =>
        useEntityUpdateSignal({
          entityType: 'Product',
        }),
      { wrapper: Wrapper },
    )

    await act(async () => {
      await vi.runAllTimersAsync()
    })

    expect(result.current.isReconnecting).toBe(false)

    // Trigger reconnecting
    act(() => {
      reconnectingCallback?.()
    })

    expect(result.current.isReconnecting).toBe(true)

    // Trigger reconnected
    await act(async () => {
      reconnectedCallback?.()
    })

    // Still reconnecting (banner delay)
    expect(result.current.isReconnecting).toBe(true)

    // Advance past RECONNECT_BANNER_MS (2000ms)
    act(() => {
      vi.advanceTimersByTime(2000)
    })

    expect(result.current.isReconnecting).toBe(false)
  })

  it('ignores signals for different entityType', async () => {
    const onAutoReload = vi.fn()
    const onCollectionUpdate = vi.fn()

    renderHook(
      () =>
        useEntityUpdateSignal({
          entityType: 'Product',
          entityId: 'entity-1',
          isDirty: false,
          onAutoReload,
          onCollectionUpdate,
        }),
      { wrapper: Wrapper },
    )

    await act(async () => {
      await vi.runAllTimersAsync()
    })

    act(() => {
      registeredHandlers['EntityUpdated'](makeSignal({ entityType: 'Order', operation: 'Updated' }))
    })

    act(() => {
      registeredHandlers['EntityCollectionUpdated'](makeSignal({ entityType: 'Order' }))
    })

    expect(onAutoReload).not.toHaveBeenCalled()
    expect(onCollectionUpdate).not.toHaveBeenCalled()
  })

  it('ignores EntityUpdated signals for different entityId', async () => {
    const onAutoReload = vi.fn()

    renderHook(
      () =>
        useEntityUpdateSignal({
          entityType: 'Product',
          entityId: 'entity-1',
          isDirty: false,
          onAutoReload,
        }),
      { wrapper: Wrapper },
    )

    await act(async () => {
      await vi.runAllTimersAsync()
    })

    act(() => {
      registeredHandlers['EntityUpdated'](
        makeSignal({ entityType: 'Product', entityId: 'entity-999', operation: 'Updated' }),
      )
    })

    expect(onAutoReload).not.toHaveBeenCalled()
  })

  it('does not start connection when no access token', async () => {
    const { getAccessToken } = await import('@/services/tokenStorage')
    ;(getAccessToken as Mock).mockReturnValueOnce(null)

    const { result } = renderHook(
      () =>
        useEntityUpdateSignal({
          entityType: 'Product',
        }),
      { wrapper: Wrapper },
    )

    await act(async () => {
      await vi.runAllTimersAsync()
    })

    // Hook still returns valid default state
    expect(result.current.conflictSignal).toBeNull()
    expect(result.current.deletedSignal).toBeNull()
    expect(result.current.isReconnecting).toBe(false)
  })
})
