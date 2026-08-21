import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

/**
 * Merges Tailwind class lists, resolving conflicting utility classes
 * (e.g. "p-2 p-4" -> "p-4") the way shadcn/ui components expect.
 */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
