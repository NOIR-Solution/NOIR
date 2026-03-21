import { renderHook } from '@testing-library/react'
import { useMediaQuery, useIsMobile } from './useMediaQuery'

describe('useMediaQuery', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('returns false by default (matchMedia mock returns matches: false)', () => {
    const { result } = renderHook(() => useMediaQuery('(max-width: 768px)'))
    expect(result.current).toBe(false)
  })

  it('returns true when matchMedia mock returns matches: true', () => {
    vi.mocked(window.matchMedia).mockImplementation((query: string) => ({
      matches: true,
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    }))

    const { result } = renderHook(() => useMediaQuery('(max-width: 768px)'))
    expect(result.current).toBe(true)
  })

  it('calls matchMedia with the provided query', () => {
    renderHook(() => useMediaQuery('(min-width: 1024px)'))
    expect(window.matchMedia).toHaveBeenCalledWith('(min-width: 1024px)')
  })

  it('registers event listener for changes', () => {
    const addEventListener = vi.fn()
    const removeEventListener = vi.fn()

    vi.mocked(window.matchMedia).mockImplementation((query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener,
      removeEventListener,
      dispatchEvent: vi.fn(),
    }))

    const { unmount } = renderHook(() => useMediaQuery('(max-width: 768px)'))
    expect(addEventListener).toHaveBeenCalledWith('change', expect.any(Function))

    unmount()
    expect(removeEventListener).toHaveBeenCalledWith('change', expect.any(Function))
  })
})

describe('useIsMobile', () => {
  it('returns a boolean', () => {
    const { result } = renderHook(() => useIsMobile())
    expect(typeof result.current).toBe('boolean')
  })

  it('calls matchMedia with mobile breakpoint', () => {
    renderHook(() => useIsMobile())
    expect(window.matchMedia).toHaveBeenCalledWith('(max-width: 767px)')
  })
})

