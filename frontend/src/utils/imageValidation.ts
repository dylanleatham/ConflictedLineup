export interface ValidationResult {
  valid: boolean;
  error?: string;
}

const VALID_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
const MAX_SIZE_BYTES = 5 * 1024 * 1024; // 5MB
const MAX_DIMENSION = 1568; // Claude's optimal size

export function validateImageFile(file: File): ValidationResult {
  // Type validation
  if (!VALID_TYPES.includes(file.type)) {
    return {
      valid: false,
      error: `Invalid file type "${file.type}". Use PNG, JPG, or WebP.`
    };
  }

  // Size validation
  if (file.size > MAX_SIZE_BYTES) {
    const sizeMB = (file.size / 1024 / 1024).toFixed(1);
    return {
      valid: false,
      error: `File too large (${sizeMB}MB). Maximum size is 5MB.`
    };
  }

  return { valid: true };
}

export async function optimizeImage(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const img = new Image();
    const reader = new FileReader();

    reader.onload = (e) => {
      img.src = e.target?.result as string;
    };

    reader.onerror = () => reject(new Error('Failed to read file'));

    img.onload = () => {
      let { width, height } = img;

      // Resize if needed
      if (width > MAX_DIMENSION || height > MAX_DIMENSION) {
        if (width > height) {
          height = (height / width) * MAX_DIMENSION;
          width = MAX_DIMENSION;
        } else {
          width = (width / height) * MAX_DIMENSION;
          height = MAX_DIMENSION;
        }
      }

      const canvas = document.createElement('canvas');
      canvas.width = width;
      canvas.height = height;

      const ctx = canvas.getContext('2d');
      if (!ctx) {
        reject(new Error('Failed to get canvas context'));
        return;
      }

      ctx.drawImage(img, 0, 0, width, height);

      // Return base64 without data URL prefix
      const dataUrl = canvas.toDataURL(file.type, 0.9);
      const base64 = dataUrl.split(',')[1];
      resolve(base64);
    };

    img.onerror = () => reject(new Error('Failed to load image'));

    reader.readAsDataURL(file);
  });
}
