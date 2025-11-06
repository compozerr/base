import { components } from '@/generated';
import getSymbolFromCurrency from 'currency-symbol-map'

export class Price {
    static formatPrice(price: components["schemas"]["Api.Abstractions.ServerTier"]["price"]): string {
        if (!price || price.value === 0) {
            return "Free";
        }

        return `${getSymbolFromCurrency(price.currency!)}${price.value!.toFixed(2)}`;
    }
}