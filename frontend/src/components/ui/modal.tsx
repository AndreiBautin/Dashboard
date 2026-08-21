import { useEffect } from "react";
import { createPortal } from "react-dom";
import { cn } from "@/lib/utils";

/**
 * A small, dependency-free modal: an "add new value" button opens this
 * instead of a form sitting inline on the page by default. Closes on
 * Escape or clicking the overlay. Renders via a portal so it isn't clipped
 * by an ancestor's overflow/stacking context.
 */
export function Modal({
  isOpen,
  onClose,
  title,
  children,
}: {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
}) {
  useEffect(() => {
    if (!isOpen) return;

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") onClose();
    }

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return createPortal(
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div
        className="absolute inset-0 animate-fade-in bg-background/80 backdrop-blur-sm"
        onClick={onClose}
        aria-hidden="true"
      />
      <div
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className={cn(
          "relative flex w-full max-w-lg animate-scale-in flex-col gap-4 rounded-xl border border-border bg-card p-6",
          "max-h-[85vh] overflow-y-auto",
        )}
      >
        <div className="flex items-center justify-between">
          <h2 className="text-sm font-medium">{title}</h2>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close"
            className="rounded-md p-1 text-muted hover:bg-background/60 hover:text-foreground"
          >
            ✕
          </button>
        </div>
        {children}
      </div>
    </div>,
    document.body,
  );
}
