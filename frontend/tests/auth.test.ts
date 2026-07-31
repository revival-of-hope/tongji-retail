import { describe, it, expect, beforeEach, vi } from 'vitest'
import { useAuthStore } from '../store/auth'

// --- 核心修复：手动模拟浏览器环境中的 localStorage ---
const localStorageMock = (() => {
  let store: Record<string, string> = {}
  return {
    getItem: (key: string) => store[key] || null,
    setItem: (key: string, value: string) => { store[key] = value.toString() },
    removeItem: (key: string) => { delete store[key] },
    clear: () => { store = {} }
  }
})()

// 将模拟的 localStorage 挂载到全局
Object.defineProperty(global, 'localStorage', { value: localStorageMock })

describe('Frontend03 单元测试', () => {
  beforeEach(() => {
    // 每次测试前重置 Store 和模拟的存储
    useAuthStore.setState({ user: null, hydrated: false })
    localStorage.clear()
  })

  it('初始状态下 user 应该为 null', () => {
    const state = useAuthStore.getState()
    expect(state.user).toBeNull()
  })

  it('注销功能 (logout) 应该正常清空状态', () => {
    // 1. 模拟一个已登录状态
    useAuthStore.setState({ user: { username: 'magisk' } as any })
    localStorage.setItem('retail-user', JSON.stringify({ username: 'magisk' }))

    // 2. 执行注销
    useAuthStore.getState().logout()

    // 3. 验证状态和本地存储是否都空了
    expect(useAuthStore.getState().user).toBeNull()
    expect(localStorage.getItem('retail-user')).toBeNull()
  })

  it('测试数据恢复逻辑 (hydrate)', () => {
    // 模拟本地已存有数据
    localStorage.setItem('retail-user', JSON.stringify({ username: 'tester' }))
    
    // 执行恢复
    useAuthStore.getState().hydrate()
    
    expect(useAuthStore.getState().user?.username).toBe('tester')
    expect(useAuthStore.getState().hydrated).toBe(true)
  })
})