import type { ReactNode } from "react";
import { NavLink } from "react-router-dom";
import { LayoutDashboard, Dumbbell, Wallet, Users, Settings } from "lucide-react";
import { cn } from "@/lib/utils";

const navItems = [
  { to: "/", label: "Dashboard", icon: LayoutDashboard, end: true },
  { to: "/fitness", label: "Fitness", icon: Dumbbell, end: false },
  { to: "/finance", label: "Finance", icon: Wallet, end: false },
  { to: "/social", label: "Social", icon: Users, end: false },
  { to: "/settings", label: "Settings", icon: Settings, end: false },
];

export function AppShell({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-screen">
      <aside className="w-56 shrink-0 border-r border-border px-4 py-6">
        <div className="mb-8 px-2">
          <p className="text-lg font-semibold tracking-tight">Dashboard</p>
          <p className="text-xs text-muted">Monthly Executive Review</p>
        </div>
        <nav className="flex flex-col gap-1">
          {navItems.map(({ to, label, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) =>
                cn(
                  "flex items-center gap-2 rounded-md px-2 py-2 text-sm text-muted transition-[background-color,color,transform] duration-150 hover:translate-x-0.5 hover:bg-card hover:text-foreground",
                  isActive && "bg-card text-foreground",
                )
              }
            >
              <Icon size={16} />
              {label}
            </NavLink>
          ))}
        </nav>
      </aside>
      <main className="flex-1 px-10 py-8">{children}</main>
    </div>
  );
}
