/**
 * Simple DOM-to-PNG export using canvas.
 * Uses html2canvas-like approach via SVG foreignObject.
 */
export const toPng = async (element: HTMLElement): Promise<Blob> => {
  const { width, height } = element.getBoundingClientRect()
  const canvas = document.createElement('canvas')
  const scale = 2 // Higher resolution
  canvas.width = width * scale
  canvas.height = height * scale
  const ctx = canvas.getContext('2d')
  if (!ctx) throw new Error('Canvas not supported')

  // Clone the element's HTML and render via SVG foreignObject
  const clone = element.cloneNode(true) as HTMLElement
  const serialized = new XMLSerializer().serializeToString(clone)
  const svg = `
    <svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}">
      <foreignObject width="100%" height="100%">
        <div xmlns="http://www.w3.org/1999/xhtml">${serialized}</div>
      </foreignObject>
    </svg>
  `

  const img = new Image()
  const svgBlob = new Blob([svg], { type: 'image/svg+xml;charset=utf-8' })
  const url = URL.createObjectURL(svgBlob)

  return new Promise<Blob>((resolve, reject) => {
    img.onload = () => {
      ctx.scale(scale, scale)
      ctx.fillStyle = 'white'
      ctx.fillRect(0, 0, width, height)
      ctx.drawImage(img, 0, 0)
      URL.revokeObjectURL(url)
      canvas.toBlob((blob) => {
        if (blob) resolve(blob)
        else reject(new Error('Failed to create blob'))
      }, 'image/png')
    }
    img.onerror = () => {
      URL.revokeObjectURL(url)
      reject(new Error('Failed to load SVG'))
    }
    img.src = url
  })
}
