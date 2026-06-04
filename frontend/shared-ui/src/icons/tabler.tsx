import type { SVGProps } from 'react'

/**
 * Set di icone Tabler usate dal mockup Turnis (docs/turnis-mockup.html).
 * Stesso factory/convenzioni di ../icons.tsx (viewBox 24, stroke currentColor,
 * strokeWidth 2). Copiate solo le glyph effettivamente usate dal design — niente
 * pacchetto @tabler/icons per non gonfiare il bundle.
 */

type IconProps = SVGProps<SVGSVGElement> & { size?: number }

function makeIcon(d: React.ReactNode) {
  return function Icon({ size = 18, ...rest }: IconProps) {
    return (
      <svg
        width={size}
        height={size}
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth={2}
        strokeLinecap="round"
        strokeLinejoin="round"
        {...rest}
      >
        {d}
      </svg>
    )
  }
}

export const TiCalendar = makeIcon(
  <>
    <path d="M4 7a2 2 0 0 1 2 -2h12a2 2 0 0 1 2 2v12a2 2 0 0 1 -2 2h-12a2 2 0 0 1 -2 -2v-12" />
    <path d="M16 3v4" />
    <path d="M8 3v4" />
    <path d="M4 11h16" />
    <path d="M11 15h1" />
    <path d="M12 15v3" />
  </>
)

export const TiCalendarEvent = makeIcon(
  <>
    <path d="M4 7a2 2 0 0 1 2 -2h12a2 2 0 0 1 2 2v12a2 2 0 0 1 -2 2h-12a2 2 0 0 1 -2 -2l0 -12" />
    <path d="M16 3l0 4" />
    <path d="M8 3l0 4" />
    <path d="M4 11l16 0" />
    <path d="M8 15h2v2h-2l0 -2" />
  </>
)

export const TiClock = makeIcon(
  <>
    <path d="M3 12a9 9 0 1 0 18 0a9 9 0 0 0 -18 0" />
    <path d="M12 7v5l3 3" />
  </>
)

export const TiClockOff = makeIcon(
  <>
    <path d="M5.633 5.64a9 9 0 1 0 12.735 12.72m1.674 -2.32a9 9 0 0 0 -12.082 -12.082" />
    <path d="M12 7v1" />
    <path d="M3 3l18 18" />
  </>
)

export const TiBell = makeIcon(
  <>
    <path d="M10 5a2 2 0 1 1 4 0a7 7 0 0 1 4 6v3a4 4 0 0 0 2 3h-16a4 4 0 0 0 2 -3v-3a7 7 0 0 1 4 -6" />
    <path d="M9 17v1a3 3 0 0 0 6 0v-1" />
  </>
)

export const TiInbox = makeIcon(
  <>
    <path d="M4 6a2 2 0 0 1 2 -2h12a2 2 0 0 1 2 2v12a2 2 0 0 1 -2 2h-12a2 2 0 0 1 -2 -2l0 -12" />
    <path d="M4 13h3l3 3h4l3 -3h3" />
  </>
)

export const TiUsers = makeIcon(
  <>
    <path d="M5 7a4 4 0 1 0 8 0a4 4 0 1 0 -8 0" />
    <path d="M3 21v-2a4 4 0 0 1 4 -4h4a4 4 0 0 1 4 4v2" />
    <path d="M16 3.13a4 4 0 0 1 0 7.75" />
    <path d="M21 21v-2a4 4 0 0 0 -3 -3.85" />
  </>
)

export const TiUser = makeIcon(
  <>
    <path d="M8 7a4 4 0 1 0 8 0a4 4 0 0 0 -8 0" />
    <path d="M6 21v-2a4 4 0 0 1 4 -4h4a4 4 0 0 1 4 4v2" />
  </>
)

export const TiUserPlus = makeIcon(
  <>
    <path d="M8 7a4 4 0 1 0 8 0a4 4 0 0 0 -8 0" />
    <path d="M16 19h6" />
    <path d="M19 16v6" />
    <path d="M6 21v-2a4 4 0 0 1 4 -4h4" />
  </>
)

export const TiHome = makeIcon(
  <>
    <path d="M5 12l-2 0l9 -9l9 9l-2 0" />
    <path d="M5 12v7a2 2 0 0 0 2 2h10a2 2 0 0 0 2 -2v-7" />
    <path d="M9 21v-6a2 2 0 0 1 2 -2h2a2 2 0 0 1 2 2v6" />
  </>
)

export const TiPlus = makeIcon(
  <>
    <path d="M12 5l0 14" />
    <path d="M5 12l14 0" />
  </>
)

export const TiCheck = makeIcon(<path d="M5 12l5 5l10 -10" />)

export const TiX = makeIcon(
  <>
    <path d="M18 6l-12 12" />
    <path d="M6 6l12 12" />
  </>
)

export const TiAlertTriangle = makeIcon(
  <>
    <path d="M12 9v4" />
    <path d="M10.363 3.591l-8.106 13.534a1.914 1.914 0 0 0 1.636 2.871h16.214a1.914 1.914 0 0 0 1.636 -2.87l-8.106 -13.536a1.914 1.914 0 0 0 -3.274 0" />
    <path d="M12 16h.01" />
  </>
)

export const TiInfoCircle = makeIcon(
  <>
    <path d="M3 12a9 9 0 1 0 18 0a9 9 0 0 0 -18 0" />
    <path d="M12 9h.01" />
    <path d="M11 12h1v4h1" />
  </>
)

export const TiChevronDown = makeIcon(<path d="M6 9l6 6l6 -6" />)
export const TiChevronUp = makeIcon(<path d="M6 15l6 -6l6 6" />)
export const TiChevronLeft = makeIcon(<path d="M15 6l-6 6l6 6" />)
export const TiChevronRight = makeIcon(<path d="M9 6l6 6l-6 6" />)
export const TiArrowLeft = makeIcon(
  <>
    <path d="M5 12l14 0" />
    <path d="M5 12l6 6" />
    <path d="M5 12l6 -6" />
  </>
)
export const TiArrowRight = makeIcon(
  <>
    <path d="M5 12l14 0" />
    <path d="M13 18l6 -6" />
    <path d="M13 6l6 6" />
  </>
)

export const TiSearch = makeIcon(
  <>
    <path d="M3 10a7 7 0 1 0 14 0a7 7 0 1 0 -14 0" />
    <path d="M21 21l-6 -6" />
  </>
)

export const TiSend = makeIcon(
  <>
    <path d="M10 14l11 -11" />
    <path d="M21 3l-6.5 18a.55 .55 0 0 1 -1 0l-3.5 -7l-7 -3.5a.55 .55 0 0 1 0 -1l18 -6.5" />
  </>
)

export const TiCopy = makeIcon(
  <>
    <path d="M7 9.667a2.667 2.667 0 0 1 2.667 -2.667h8.666a2.667 2.667 0 0 1 2.667 2.667v8.666a2.667 2.667 0 0 1 -2.667 2.667h-8.666a2.667 2.667 0 0 1 -2.667 -2.667l0 -8.666" />
    <path d="M4.012 16.737a2.005 2.005 0 0 1 -1.012 -1.737v-10c0 -1.1 .9 -2 2 -2h10c.75 0 1.158 .385 1.5 1" />
  </>
)

export const TiEdit = makeIcon(
  <>
    <path d="M7 7h-1a2 2 0 0 0 -2 2v9a2 2 0 0 0 2 2h9a2 2 0 0 0 2 -2v-1" />
    <path d="M20.385 6.585a2.1 2.1 0 0 0 -2.97 -2.97l-8.415 8.385v3h3l8.385 -8.415" />
    <path d="M16 5l3 3" />
  </>
)

export const TiRepeat = makeIcon(
  <>
    <path d="M4 12v-3a3 3 0 0 1 3 -3h13m-3 -3l3 3l-3 3" />
    <path d="M20 12v3a3 3 0 0 1 -3 3h-13m3 3l-3 -3l3 -3" />
  </>
)

export const TiMessageCircle = makeIcon(
  <path d="M3 20l1.3 -3.9c-2.324 -3.437 -1.426 -7.872 2.1 -10.374c3.526 -2.501 8.59 -2.296 11.845 .48c3.255 2.777 3.695 7.266 1.029 10.501c-2.666 3.235 -7.615 4.215 -11.574 2.293l-4.7 1" />
)

export const TiMapPin = makeIcon(
  <>
    <path d="M9 11a3 3 0 1 0 6 0a3 3 0 0 0 -6 0" />
    <path d="M17.657 16.657l-4.243 4.243a2 2 0 0 1 -2.827 0l-4.244 -4.243a8 8 0 1 1 11.314 0" />
  </>
)

export const TiMapPinOff = makeIcon(
  <>
    <path d="M9.442 9.432a3 3 0 0 0 4.113 4.134m1.445 -2.566a3 3 0 0 0 -3 -3" />
    <path d="M17.152 17.162l-3.738 3.738a2 2 0 0 1 -2.827 0l-4.244 -4.243a8 8 0 0 1 -.476 -10.794m2.18 -1.82a8.003 8.003 0 0 1 10.91 10.912" />
    <path d="M3 3l18 18" />
  </>
)

export const TiCurrentLocation = makeIcon(
  <>
    <path d="M9 12a3 3 0 1 0 6 0a3 3 0 1 0 -6 0" />
    <path d="M4 12a8 8 0 1 0 16 0a8 8 0 1 0 -16 0" />
    <path d="M12 2l0 2" />
    <path d="M12 20l0 2" />
    <path d="M20 12l2 0" />
    <path d="M2 12l2 0" />
  </>
)

export const TiBeach = makeIcon(
  <>
    <path d="M17.553 16.75a7.5 7.5 0 0 0 -10.606 0" />
    <path d="M18 3.804a6 6 0 0 0 -8.196 2.196l10.392 6a6 6 0 0 0 -2.196 -8.196" />
    <path d="M16.732 10c1.658 -2.87 2.225 -5.644 1.268 -6.196c-.957 -.552 -3.075 1.326 -4.732 4.196" />
    <path d="M15 9l-3 5.196" />
    <path d="M3 19.25a2.4 2.4 0 0 1 1 -.25a2.4 2.4 0 0 1 2 1a2.4 2.4 0 0 0 2 1a2.4 2.4 0 0 0 2 -1a2.4 2.4 0 0 1 2 -1a2.4 2.4 0 0 1 2 1a2.4 2.4 0 0 0 2 1a2.4 2.4 0 0 0 2 -1a2.4 2.4 0 0 1 2 -1a2.4 2.4 0 0 1 1 .25" />
  </>
)

export const TiBrandWhatsapp = makeIcon(
  <>
    <path d="M3 21l1.65 -3.8a9 9 0 1 1 3.4 2.9l-5.05 .9" />
    <path d="M9 10a.5 .5 0 0 0 1 0v-1a.5 .5 0 0 0 -1 0v1a5 5 0 0 0 5 5h1a.5 .5 0 0 0 0 -1h-1a.5 .5 0 0 0 0 1" />
  </>
)

export const TiSparkles = makeIcon(
  <path d="M16 18a2 2 0 0 1 2 2a2 2 0 0 1 2 -2a2 2 0 0 1 -2 -2a2 2 0 0 1 -2 2m0 -12a2 2 0 0 1 2 2a2 2 0 0 1 2 -2a2 2 0 0 1 -2 -2a2 2 0 0 1 -2 2m-7 12a6 6 0 0 1 6 -6a6 6 0 0 1 -6 -6a6 6 0 0 1 -6 6a6 6 0 0 1 6 6" />
)

export const TiMenu2 = makeIcon(
  <>
    <path d="M4 6l16 0" />
    <path d="M4 12l16 0" />
    <path d="M4 18l16 0" />
  </>
)

export const TiBuildingStore = makeIcon(
  <>
    <path d="M3 21l18 0" />
    <path d="M3 7v1a3 3 0 0 0 6 0v-1m0 1a3 3 0 0 0 6 0v-1m0 1a3 3 0 0 0 6 0v-1h-18l2 -4h14l2 4" />
    <path d="M5 21l0 -10.15" />
    <path d="M19 21l0 -10.15" />
    <path d="M9 21v-4a2 2 0 0 1 2 -2h2a2 2 0 0 1 2 2v4" />
  </>
)

export const TiBriefcase = makeIcon(
  <>
    <path d="M3 7m0 2a2 2 0 0 1 2 -2h14a2 2 0 0 1 2 2v9a2 2 0 0 1 -2 2h-14a2 2 0 0 1 -2 -2z" />
    <path d="M8 7v-2a2 2 0 0 1 2 -2h4a2 2 0 0 1 2 2v2" />
    <path d="M12 12l0 .01" />
    <path d="M3 13a20 20 0 0 0 18 0" />
  </>
)

export const TiPackage = makeIcon(
  <>
    <path d="M12 3l8 4.5l0 9l-8 4.5l-8 -4.5l0 -9l8 -4.5" />
    <path d="M12 12l8 -4.5" />
    <path d="M12 12l0 9" />
    <path d="M12 12l-8 -4.5" />
    <path d="M16 5.25l-8 4.5" />
  </>
)

export const TiFileText = makeIcon(
  <>
    <path d="M14 3v4a1 1 0 0 0 1 1h4" />
    <path d="M17 21h-10a2 2 0 0 1 -2 -2v-14a2 2 0 0 1 2 -2h7l5 5v11a2 2 0 0 1 -2 2z" />
    <path d="M9 9l1 0" />
    <path d="M9 13l6 0" />
    <path d="M9 17l6 0" />
  </>
)
