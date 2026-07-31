// frontend/lib/api/sdk.ts

/**
 * 模拟后端 API SDK
 */
const mockUser = {
  id: 1,
  username: "customer",
  email: "customer@retail.local",
  phone: null,
  role: "Customer" as const, // 使用 const 确保类型匹配字面量
  isActive: true,
  createdAt: new Date().toISOString(),
  merchant: null,
};

export const api = {
  // 模拟登录接口
  login: async (data: any) => {
    return {
      accessToken: "mock-token-123",
      user: { ...mockUser, username: data.username }
    };
  },
  
  // 模拟注册接口
  register: async (data: any) => {
    return {
      accessToken: "mock-token-reg",
      user: { ...mockUser, username: data.username, email: data.email || mockUser.email }
    };
  },
  
  // 模拟获取个人信息接口
  me: async () => {
    return mockUser;
  }
};