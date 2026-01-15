/// <reference types="vite/client" />

// Allow importing CSS files
declare module '*.css' {
  const content: string
  export default content
}

// Version info injected at build time
declare const __APP_VERSION__: string;
declare const __GIT_COMMIT__: string;
declare const __GIT_BRANCH__: string;
declare const __BUILD_TIME__: string;
