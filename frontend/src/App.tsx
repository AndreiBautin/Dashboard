import { BrowserRouter, Routes, Route } from "react-router-dom";
import { toRouterBasename } from "@/lib/config";
import { AppShell } from "@/components/layout/AppShell";
import { DashboardPage } from "@/features/dashboard/DashboardPage";
import { FitnessPage } from "@/features/fitness/FitnessPage";
import { FinancePage } from "@/features/finance/FinancePage";
import { SocialPage } from "@/features/social/SocialPage";
import { SettingsPage } from "@/features/settings/SettingsPage";

function App() {
  return (
    <BrowserRouter basename={toRouterBasename(import.meta.env.BASE_URL)}>
      <AppShell>
        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/fitness" element={<FitnessPage />} />
          <Route path="/finance" element={<FinancePage />} />
          <Route path="/social" element={<SocialPage />} />
          <Route path="/settings" element={<SettingsPage />} />
        </Routes>
      </AppShell>
    </BrowserRouter>
  );
}

export default App;
