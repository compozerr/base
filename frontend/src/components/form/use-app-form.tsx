import { createFormHook, createFormHookContexts } from "@tanstack/react-form"
import TextField from "./text-field"
import SelectField from "./select-field"

const { fieldContext, formContext, useFieldContext } = createFormHookContexts()

export const useAppFieldContext = useFieldContext;
export const { useAppForm } = createFormHook({
    fieldComponents: {
        TextField,
        SelectField
    },
    formComponents: {
    },
    fieldContext,
    formContext,
})
