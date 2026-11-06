export default {
  content: ["./index.html", "./src/**/*.{js,jsx}"],
  theme: {
    extend: {
      colors: {
        primary: {
          DEFAULT: "#C1272D",
          dark: "#A01E24",
          light: "#E8A0A3",
        },
        secondary: {
          DEFAULT: "#F8BBD0",
          dark: "#F48FB1",
        },
        accent: {
          DEFAULT: "#F2C14E",
          dark: "#E6A532",
        },
        success: "#2E7D32",
        info: "#1976D2",
        danger: "#C62828",
        warning: "#EF6C00",
      },
      borderRadius: {
        xl: "0.75rem",
        "2xl": "1rem",
      },
      boxShadow: {
        card: "0 1px 3px rgba(0,0,0,0.08), 0 4px 12px rgba(0,0,0,0.05)",
      },
      fontFamily: {
        sans: ["Inter", "system-ui", "sans-serif"],
        display: ["Poppins", "sans-serif"],
      },
    },
  },
  plugins: [],
}
