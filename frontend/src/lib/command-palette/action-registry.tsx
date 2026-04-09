import {
  createContext,
  useContext,
  useEffect,
  useRef,
  useSyncExternalStore,
  type DependencyList,
  type ReactNode,
} from "react"
import type { Action } from "./types"

type Listener = () => void

class ActionStore {
  private actions = new Map<string, Action>()
  private listeners = new Set<Listener>()

  subscribe = (listener: Listener) => {
    this.listeners.add(listener)
    return () => this.listeners.delete(listener)
  }

  getSnapshot = (): ReadonlyMap<string, Action> => {
    return this.actions
  }

  register(actions: Action[]): () => void {
    for (const action of actions) {
      this.actions.set(action.id, action)
    }
    this.notify()
    return () => {
      for (const action of actions) {
        this.actions.delete(action.id)
      }
      this.notify()
    }
  }

  private notify() {
    this.actions = new Map(this.actions)
    for (const listener of this.listeners) {
      listener()
    }
  }
}

const ActionRegistryContext = createContext<ActionStore | null>(null)

export function ActionRegistryProvider({ children }: { children: ReactNode }) {
  const storeRef = useRef<ActionStore>(null)
  if (!storeRef.current) {
    storeRef.current = new ActionStore()
  }

  return (
    <ActionRegistryContext.Provider value={storeRef.current}>
      {children}
    </ActionRegistryContext.Provider>
  )
}

function useActionStore(): ActionStore {
  const store = useContext(ActionRegistryContext)
  if (!store) {
    throw new Error("useRegisterActions must be used within ActionRegistryProvider")
  }
  return store
}

export function useRegisterActions(actions: Action[], deps: DependencyList = []) {
  const store = useActionStore()

  useEffect(() => {
    return store.register(actions)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps)
}

export function useActions(): Action[] {
  const store = useActionStore()
  const actionsMap = useSyncExternalStore(store.subscribe, store.getSnapshot)

  return Array.from(actionsMap.values()).filter((action) => {
    if (action.enabled === undefined) return true
    if (typeof action.enabled === "function") return action.enabled()
    return action.enabled
  })
}
