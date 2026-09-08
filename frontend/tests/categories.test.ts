import { describe, expect, it } from "vitest"
import { flattenCategories } from "@/lib/categories"

const categories = [
  {
    id: 1,
    name: "数码",
    sortOrder: 1,
    children: [
      {
        id: 2,
        name: "电脑",
        sortOrder: 1,
        children: [
          { id: 3, name: "笔记本", sortOrder: 1, children: [] },
        ],
      },
    ],
  },
]

describe("flattenCategories", () => {
  it("preserves hierarchy order and depth", () => {
    expect(flattenCategories(categories)).toEqual([
      { id: 1, label: "数码", depth: 0 },
      { id: 2, label: "　└ 电脑", depth: 1 },
      { id: 3, label: "　　└ 笔记本", depth: 2 },
    ])
  })
})
