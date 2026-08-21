import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import App from "./App";

describe("App shell", () => {
  it("renders the nav and the dashboard page by default", () => {
    render(<App />);

    // "Dashboard" is both the app's name in the header and the label of the
    // first nav item, so this has to say which one it means. The brand is the
    // one that isn't a link.
    expect(screen.getByText("Dashboard", { selector: "p" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /dashboard/i })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Monthly Executive Review" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /fitness/i })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /finance/i })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /social/i })).toBeInTheDocument();
  });
});
