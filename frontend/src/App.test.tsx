import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import App from "./App";

describe("App shell", () => {
  it("renders the nav and the dashboard page by default", () => {
    render(<App />);

    expect(screen.getByText("Dashboard")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /overview/i })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Monthly Executive Review" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /fitness/i })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /finance/i })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /social/i })).toBeInTheDocument();
  });
});
