// frontend/lib/api/generated/types.gen.ts

export type UserSummary = {
  id: number;
  username: string;
  email: string;
  phone: string | null;
  role: "Customer" | "Merchant" | "Admin" | "Staff";
  isActive: boolean;
  createdAt: string;
  merchant: any | null;
};