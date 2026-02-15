interface SaleOffer {
  name: string
  badge: string
  discount: string
  salePrice: number
  originalPrice: number
  badgeColor: string
  buttonColor: string
  buttonHoverColor: string
  priceColor: string
}

interface SeasonalEvent {
  name: string
  badge: string
  discount: string
  /** Month (0-indexed), day */
  start: [number, number]
  /** Month (0-indexed), day */
  end: [number, number]
  badgeColor: string
  buttonColor: string
  buttonHoverColor: string
  priceColor: string
}

const SALE_PRICE = 5
const ORIGINAL_PRICE = 8

const events: SeasonalEvent[] = [
  {
    name: "New Year Sale",
    badge: "🎆 New Year Sale",
    discount: "37% OFF",
    start: [0, 1],
    end: [0, 15],
    badgeColor: "bg-yellow-600 hover:bg-yellow-700",
    buttonColor: "bg-yellow-600 hover:bg-yellow-700",
    buttonHoverColor: "hover:bg-yellow-700",
    priceColor: "text-yellow-500",
  },
  {
    name: "Winter Sale",
    badge: "❄️ Winter Sale",
    discount: "37% OFF",
    start: [0, 16],
    end: [1, 13],
    badgeColor: "bg-sky-600 hover:bg-sky-700",
    buttonColor: "bg-sky-600 hover:bg-sky-700",
    buttonHoverColor: "hover:bg-sky-700",
    priceColor: "text-sky-500",
  },
  {
    name: "Valentine's Deal",
    badge: "💝 Valentine's Deal",
    discount: "37% OFF",
    start: [1, 14],
    end: [1, 28],
    badgeColor: "bg-pink-600 hover:bg-pink-700",
    buttonColor: "bg-pink-600 hover:bg-pink-700",
    buttonHoverColor: "hover:bg-pink-700",
    priceColor: "text-pink-500",
  },
  {
    name: "Spring Sale",
    badge: "🌸 Spring Sale",
    discount: "37% OFF",
    start: [2, 1],
    end: [3, 30],
    badgeColor: "bg-emerald-600 hover:bg-emerald-700",
    buttonColor: "bg-emerald-600 hover:bg-emerald-700",
    buttonHoverColor: "hover:bg-emerald-700",
    priceColor: "text-emerald-500",
  },
  {
    name: "May Day Sale",
    badge: "🌼 May Day Sale",
    discount: "37% OFF",
    start: [4, 1],
    end: [4, 31],
    badgeColor: "bg-lime-600 hover:bg-lime-700",
    buttonColor: "bg-lime-600 hover:bg-lime-700",
    buttonHoverColor: "hover:bg-lime-700",
    priceColor: "text-lime-500",
  },
  {
    name: "Summer Sale",
    badge: "☀️ Summer Sale",
    discount: "37% OFF",
    start: [5, 1],
    end: [7, 31],
    badgeColor: "bg-orange-600 hover:bg-orange-700",
    buttonColor: "bg-orange-600 hover:bg-orange-700",
    buttonHoverColor: "hover:bg-orange-700",
    priceColor: "text-orange-500",
  },
  {
    name: "Back to Work Sale",
    badge: "🚀 Back to Work Sale",
    discount: "37% OFF",
    start: [8, 1],
    end: [9, 15],
    badgeColor: "bg-indigo-600 hover:bg-indigo-700",
    buttonColor: "bg-indigo-600 hover:bg-indigo-700",
    buttonHoverColor: "hover:bg-indigo-700",
    priceColor: "text-indigo-500",
  },
  {
    name: "Autumn Sale",
    badge: "🍂 Autumn Sale",
    discount: "37% OFF",
    start: [9, 16],
    end: [10, 24],
    badgeColor: "bg-amber-600 hover:bg-amber-700",
    buttonColor: "bg-amber-600 hover:bg-amber-700",
    buttonHoverColor: "hover:bg-amber-700",
    priceColor: "text-amber-500",
  },
  {
    name: "Black Friday Deal",
    badge: "🔥 Black Friday Deal",
    discount: "37% OFF",
    start: [10, 25],
    end: [11, 2],
    badgeColor: "bg-red-600 hover:bg-red-700",
    buttonColor: "bg-red-600 hover:bg-red-700",
    buttonHoverColor: "hover:bg-red-700",
    priceColor: "text-red-500",
  },
  {
    name: "Holiday Sale",
    badge: "🎄 Holiday Sale",
    discount: "37% OFF",
    start: [11, 3],
    end: [11, 31],
    badgeColor: "bg-red-600 hover:bg-red-700",
    buttonColor: "bg-red-600 hover:bg-red-700",
    buttonHoverColor: "hover:bg-red-700",
    priceColor: "text-red-500",
  },
]

function getCurrentEvent(): SeasonalEvent {
  const now = new Date()
  const month = now.getMonth()
  const day = now.getDate()

  for (const event of events) {
    const [startMonth, startDay] = event.start
    const [endMonth, endDay] = event.end

    if (startMonth <= endMonth) {
      // Same year range
      if (
        (month > startMonth || (month === startMonth && day >= startDay)) &&
        (month < endMonth || (month === endMonth && day <= endDay))
      ) {
        return event
      }
    } else {
      // Wraps around year end (e.g. Dec 25 - Jan 5)
      if (
        (month > startMonth || (month === startMonth && day >= startDay)) ||
        (month < endMonth || (month === endMonth && day <= endDay))
      ) {
        return event
      }
    }
  }

  // Fallback — should never happen since events cover the full year
  return events[0]!
}

export function useSaleOffer(): SaleOffer {
  const event = getCurrentEvent()

  return {
    name: event.name,
    badge: event.badge,
    discount: event.discount,
    salePrice: SALE_PRICE,
    originalPrice: ORIGINAL_PRICE,
    badgeColor: event.badgeColor,
    buttonColor: event.buttonColor,
    buttonHoverColor: event.buttonHoverColor,
    priceColor: event.priceColor,
  }
}

/** Non-hook version for use outside React components (e.g. static data) */
export function getSaleOffer(): SaleOffer {
  const event = getCurrentEvent()

  return {
    name: event.name,
    badge: event.badge,
    discount: event.discount,
    salePrice: SALE_PRICE,
    originalPrice: ORIGINAL_PRICE,
    badgeColor: event.badgeColor,
    buttonColor: event.buttonColor,
    buttonHoverColor: event.buttonHoverColor,
    priceColor: event.priceColor,
  }
}
