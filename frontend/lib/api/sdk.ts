// frontend/lib/api/sdk.ts
export const api = {
  login: async (data: any) => {
    // 模拟后端返回
    return {
      accessToken: "mock-token-" + Date.now(),
      user: { username: data.username, id: "mock-id-03" }
    };
  },
  register: async (data: any) => {
    return {
      accessToken: "mock-token-reg",
      user: { username: data.username, id: "mock-id-new" }
    };
  },
  me: async () => {
    return { username: "magisk_tester", id: "mock-id-03" };
  }
};