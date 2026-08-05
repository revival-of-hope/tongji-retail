import type { CategoryResponse } from "@/lib/api/generated/types.gen"

export type FlatCategory = {
  id: number
  label: string
  depth: number
}

export function flattenCategories(
  categories: CategoryResponse[],
  depth = 0,
): FlatCategory[] {
  return categories.flatMap((category) => [
    {
      id: category.id,
      label: `${"　".repeat(depth)}${depth > 0 ? "└ " : ""}${category.name}`,
      depth,
    },
    ...flattenCategories(category.children, depth + 1),
  ])
}
