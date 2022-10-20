import java.awt.AlphaComposite;
import java.awt.Color;
import java.awt.Desktop.Action;
import java.awt.Graphics2D;
import java.awt.image.BufferedImage;
import java.io.File;
import java.io.IOException;
import java.util.ArrayList;
import java.util.concurrent.Semaphore;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReentrantLock;

import javax.imageio.ImageIO;

public class MandelbrotColor {
	private static int TOTAL_WIDTH = 1080, TOTAL_HEIGHT = 1080, max = 100000;
	private static BufferedImage CANVAS = new BufferedImage(TOTAL_WIDTH, TOTAL_HEIGHT, BufferedImage.TYPE_INT_ARGB);
	private static Graphics2D g2d = (Graphics2D) CANVAS.getGraphics();
	private static ArrayList<Square> squareMatrix = new ArrayList<Square>();
	private static int[] colors;
	private static double startTime, endTime;
	private static Semaphore sem_isUsing;

	public static void main(String[] args) throws Exception {
		AlphaComposite composite = AlphaComposite.getInstance(AlphaComposite.CLEAR, 0.0f);
		g2d.setComposite(composite);
		g2d.setColor(new Color(0, 0, 0, 0));
		g2d.fillRect(0, 0, 10, 10);
		sem_isUsing = new Semaphore(1);

		DataMatrix();
		FillColorArray();
		EmptyCanvas(CANVAS);
		PaintMandelbrotSequential();
		PaintMandelbrotParallel();
	}

	private static void PaintMandelbrotParallel() throws IOException, InterruptedException {
		startTime = System.nanoTime();

		squareMatrix.parallelStream().forEach(e -> {
			try {
				paintSquare(TOTAL_WIDTH, TOTAL_HEIGHT, CANVAS, max, e.xStart, e.xEnd, e.yStart, e.yEnd);
				sem_isUsing.acquire();
				ImageIO.write(CANVAS, "png", new File("MandelbrotParallel.png"));
				sem_isUsing.release();
			} catch (IOException | InterruptedException e1) {
				e1.printStackTrace();
			}
		});

		endTime = System.nanoTime();
		System.out.println("Parallel Time = " + (endTime - startTime) / 1000000000 + " seconds");
	}

	private static void PaintMandelbrotSequential() throws IOException, InterruptedException {
		startTime = System.nanoTime();
		for (int i = 0; i < squareMatrix.size(); i++) {
			paintSquare(TOTAL_WIDTH, TOTAL_HEIGHT, CANVAS, max, squareMatrix.get(i).xStart, squareMatrix.get(i).xEnd,
					squareMatrix.get(i).yStart, squareMatrix.get(i).yEnd);
			ImageIO.write(CANVAS, "png", new File("MandelbrotSequential.png"));
		}
		endTime = System.nanoTime();
		
		System.out.println("Sequential Time = " + (double)(endTime - startTime) / 1000000000 + " seconds");
	}

	private static void paintSquare(int width, int height, BufferedImage image, int max, int initWidth, int finalWidth,
			int initHeight, int finalHeight) throws IOException, InterruptedException {

		for (int row = initHeight; row < finalHeight; row++) {
			for (int col = initWidth; col < finalWidth; col++) {
				double nReal = (col - width / 2.0) * 4.0 / width;
				double nImaginary = (row - height / 2.0) * 4.0 / height;
				double x = 0, y = 0;
				int iteration = 0;
				while ((x * x + y * y) < 4 && iteration < max) {
					double xTemp = (x * x) - (y * y) + nReal;
					y = 2.0 * x * y + nImaginary;
					x = xTemp;
					iteration++;
				}
				if (iteration < max) {
					sem_isUsing.acquire();
					image.setRGB(col, row, colors[iteration]);
					sem_isUsing.release();
				}
			}
		}

		BufferedImage finalImage = image.getSubimage(initWidth, initHeight, TOTAL_WIDTH - initWidth,
				TOTAL_HEIGHT - initHeight);
		g2d.drawImage(finalImage, TOTAL_WIDTH, TOTAL_HEIGHT, null);
	}

	private static void EmptyCanvas(BufferedImage image) throws InterruptedException, IOException {
		Color black = new Color(0, 0, 0);

		for (int row = 0; row < squareMatrix.size(); row++) {
			for (int col = 0; col < squareMatrix.size(); col++) {
				image.setRGB(col, row, black.getRGB());
			}
		}

		ImageIO.write(CANVAS, "png", new File("MandelbrotParallel.png"));
		Thread.sleep(5000);
	}
	
	private static void FillColorArray() {
		colors = new int[max];
		for (int i = 0; i < max; i++) {
			colors[i] = Color.HSBtoRGB(i / 256f, 1, i / (i + 8f));
		}
	}

	private static void DataMatrix() {
		squareMatrix.add(new Square(0, TOTAL_WIDTH / 4, 0, TOTAL_HEIGHT / 4));
		squareMatrix.add(new Square(TOTAL_WIDTH / 4, TOTAL_WIDTH / 2, 0, TOTAL_HEIGHT / 4));
		squareMatrix.add(new Square(TOTAL_WIDTH / 2, 3 * TOTAL_WIDTH / 4, 0, TOTAL_HEIGHT / 4));
		squareMatrix.add(new Square(3 * TOTAL_WIDTH / 4, TOTAL_WIDTH, 0, TOTAL_HEIGHT / 4));

		squareMatrix.add(new Square(0, TOTAL_WIDTH / 4, TOTAL_HEIGHT / 4, TOTAL_HEIGHT / 2));
		squareMatrix.add(new Square(TOTAL_WIDTH / 4, TOTAL_WIDTH / 2, TOTAL_HEIGHT / 4, TOTAL_HEIGHT / 2));
		squareMatrix.add(new Square(TOTAL_WIDTH / 2, 3 * TOTAL_WIDTH / 4, TOTAL_HEIGHT / 4, TOTAL_HEIGHT / 2));
		squareMatrix.add(new Square(3 * TOTAL_WIDTH / 4, TOTAL_WIDTH, TOTAL_HEIGHT / 4, TOTAL_HEIGHT / 2));

		squareMatrix.add(new Square(0, TOTAL_WIDTH / 4, TOTAL_HEIGHT / 2, 3 * TOTAL_HEIGHT / 4));
		squareMatrix.add(new Square(TOTAL_WIDTH / 4, TOTAL_WIDTH / 2, TOTAL_HEIGHT / 2, 3 * TOTAL_HEIGHT / 4));
		squareMatrix.add(new Square(TOTAL_WIDTH / 2, 3 * TOTAL_WIDTH / 4, TOTAL_HEIGHT / 2, 3 * TOTAL_HEIGHT / 4));
		squareMatrix.add(new Square(3 * TOTAL_WIDTH / 4, TOTAL_WIDTH, TOTAL_HEIGHT / 2, 3 * TOTAL_HEIGHT / 4));

		squareMatrix.add(new Square(0, TOTAL_WIDTH / 4, 3 * TOTAL_HEIGHT / 4, TOTAL_HEIGHT));
		squareMatrix.add(new Square(TOTAL_WIDTH / 4, TOTAL_WIDTH / 2, 3 * TOTAL_HEIGHT / 4, TOTAL_HEIGHT));
		squareMatrix.add(new Square(TOTAL_WIDTH / 2, 3 * TOTAL_WIDTH / 4, 3 * TOTAL_HEIGHT / 4, TOTAL_HEIGHT));
		squareMatrix.add(new Square(3 * TOTAL_WIDTH / 4, TOTAL_WIDTH, 3 * TOTAL_HEIGHT / 4, TOTAL_HEIGHT));
	}
}
