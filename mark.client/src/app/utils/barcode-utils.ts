// src/app/utils/barcode-utils.ts
export class BarcodeUtils {
  /**
   * kind: '1D' | '2D'
   * type: Code128, Code39, EAN13, QRCode, DataMatrix, Aztec, MaxiCode
   * value: string
   * width/height in px
   */
  static generateCanvas(kind: '1D' | '2D', type: string, value: string, width = 200, height = 200): Promise<HTMLCanvasElement> {
    return new Promise((resolve, reject) => {
      const canvas = document.createElement('canvas');
      canvas.width = width;
      canvas.height = height;

      try {
        if (kind === '1D') {
          // JsBarcode must be loaded globally or imported
          // @ts-ignore
          if (window.JsBarcode) {
            // JsBarcode draws inside an <svg> or <canvas> and requires format mapping
            // @ts-ignore
            window.JsBarcode(canvas, value, { format: type, width: 2, height: Math.max(30, height - 20) });
            resolve(canvas);
            return;
          }
          reject(new Error('JsBarcode not available'));
          return;
        }

        // 2D (QRCode)
        if (type === 'QRCode' || type === 'QR') {
          // qrcode library should be available and expose toCanvas
          // @ts-ignore
          if (window.QRCode) {
            // @ts-ignore
            window.QRCode.toCanvas(canvas, value, { width, margin: 1 }, (err: any) => {
              if (err) reject(err);
              else resolve(canvas);
            });
            return;
          }
          // fallback: draw simple text when qrcode lib missing
          const ctx = canvas.getContext('2d')!;
          ctx.fillStyle = '#000';
          ctx.fillRect(0, 0, width, height);
          ctx.fillStyle = '#fff';
          ctx.font = '14px sans-serif';
          ctx.fillText(value.slice(0, 20), 10, 20);
          resolve(canvas);
          return;
        }

        // DataMatrix / Aztec / MaxiCode require bwip-js (recommended)
        // If you have bwip-js, you can call:
        // bwipjs.toCanvas(canvas, { bcid: 'datamatrix', text: value, scale: 2, width, height }, callback)
        // Fallback: draw value string on canvas
        const ctx = canvas.getContext('2d')!;
        ctx.fillStyle = '#fff';
        ctx.fillRect(0, 0, width, height);
        ctx.fillStyle = '#000';
        ctx.font = '12px monospace';
        ctx.fillText(`${type}: ${value}`, 6, 20);
        resolve(canvas);
      } catch (err) {
        reject(err);
      }
    });
  }
}
