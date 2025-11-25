import React, {
  createContext,
  useContext,
  useState,
  useCallback,
  ReactNode,
} from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button, type ButtonProps } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

type ModalType = "alert" | "confirm" | "prompt";

interface ModalState {
  isOpen: boolean;
  type: ModalType;
  title: string;
  message: string;
  defaultValue?: string;
  onConfirm?: (value?: string) => void;
  onCancel?: () => void;
  confirmText?: string;
  cancelText?: string;
  confirmVariant?: ButtonProps["variant"];
}

interface ModalOptions {
  confirmText?: string;
  cancelText?: string;
  variant?: ButtonProps["variant"];
}

interface ModalContextType {
  alert: (
    title: string,
    message: string,
    options?: Pick<ModalOptions, "confirmText" | "variant">
  ) => Promise<void>;
  confirm: (
    title: string,
    message: string,
    options?: ModalOptions
  ) => Promise<boolean>;
  prompt: (
    title: string,
    message: string,
    defaultValue?: string,
    options?: ModalOptions
  ) => Promise<string | null>;
}

const ModalContext = createContext<ModalContextType | undefined>(undefined);

export const ModalProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [modalState, setModalState] = useState<ModalState>({
    isOpen: false,
    type: "alert",
    title: "",
    message: "",
  });
  const [inputValue, setInputValue] = useState("");

  const closeModal = useCallback(() => {
    setModalState((prev) => ({ ...prev, isOpen: false }));
    setInputValue("");
  }, []);

  const alert = useCallback(
    (
      title: string,
      message: string,
      options?: Pick<ModalOptions, "confirmText" | "variant">
    ): Promise<void> => {
      return new Promise((resolve) => {
        setModalState({
          isOpen: true,
          type: "alert",
          title,
          message,
          onConfirm: () => {
            closeModal();
            resolve();
          },
          onCancel: () => {
            closeModal();
            resolve();
          },
          confirmText: options?.confirmText || "OK",
          confirmVariant: options?.variant,
        });
      });
    },
    [closeModal]
  );

  const confirm = useCallback(
    (
      title: string,
      message: string,
      options?: ModalOptions
    ): Promise<boolean> => {
      return new Promise((resolve) => {
        setModalState({
          isOpen: true,
          type: "confirm",
          title,
          message,
          onConfirm: () => {
            closeModal();
            resolve(true);
          },
          onCancel: () => {
            closeModal();
            resolve(false);
          },
          confirmText: options?.confirmText || "Confirm",
          cancelText: options?.cancelText || "Cancel",
          confirmVariant: options?.variant,
        });
      });
    },
    [closeModal]
  );

  const prompt = useCallback(
    (
      title: string,
      message: string,
      defaultValue: string = "",
      options?: ModalOptions
    ): Promise<string | null> => {
      return new Promise((resolve) => {
        setInputValue(defaultValue);
        setModalState({
          isOpen: true,
          type: "prompt",
          title,
          message,
          defaultValue,
          onConfirm: (value) => {
            closeModal();
            resolve(value || null);
          },
          onCancel: () => {
            closeModal();
            resolve(null);
          },
          confirmText: options?.confirmText || "Submit",
          cancelText: options?.cancelText || "Cancel",
          confirmVariant: options?.variant,
        });
      });
    },
    [closeModal]
  );

  const handleConfirm = () => {
    if (modalState.type === "prompt") {
      modalState.onConfirm?.(inputValue);
    } else {
      modalState.onConfirm?.();
    }
  };

  const handleCancel = () => {
    modalState.onCancel?.();
  };

  const handleOpenChange = (open: boolean) => {
    if (!open) {
      handleCancel();
    }
  };

  return (
    <ModalContext.Provider value={{ alert, confirm, prompt }}>
      {children}
      <Dialog open={modalState.isOpen} onOpenChange={handleOpenChange}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{modalState.title}</DialogTitle>
            <DialogDescription>{modalState.message}</DialogDescription>
          </DialogHeader>

          {modalState.type === "prompt" && (
            <div className="py-4">
              <Input
                value={inputValue}
                onChange={(e) => setInputValue(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    handleConfirm();
                  }
                }}
                placeholder="Enter value..."
                autoFocus
              />
            </div>
          )}

          <DialogFooter>
            {modalState.type !== "alert" && (
              <Button variant="outline" onClick={handleCancel}>
                {modalState.cancelText}
              </Button>
            )}
            <Button
              variant={modalState.confirmVariant}
              onClick={handleConfirm}
            >
              {modalState.confirmText}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </ModalContext.Provider>
  );
};

export const useModal = (): ModalContextType => {
  const context = useContext(ModalContext);
  if (!context) {
    throw new Error("useModal must be used within a ModalProvider");
  }
  return context;
};
