import tempfile
import threading
import unittest
from pathlib import Path
import cv2
import numpy as np
from PIL import Image
from clorium_stack import stack, align_image, StackError, Cancelled


class StackTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(prefix='clorium_stack_',dir=Path(__file__).parent)
        self.root=Path(self.temp.name)
        rng=np.random.default_rng(71)
        self.image=rng.integers(40,215,(257,513,3),dtype=np.uint8)
        self.image=cv2.GaussianBlur(self.image,(3,3),.5)
    def tearDown(self):
        assert self.root.resolve().parent==Path(__file__).resolve().parent
        self.temp.cleanup()
    def save(self,name,image,**kw):
        path=self.root/name;Image.fromarray(image).save(path,**kw);return path
    def test_identical_colour_profile_odd_size_and_collision(self):
        a=self.save('한글 a.png',self.image,icc_profile=b'test-profile')
        b=self.save('b.png',self.image,icc_profile=b'test-profile')
        out=self.root/'out.png';out.write_bytes(b'previous')
        result=stack([a,b],out)
        self.assertEqual(out.read_bytes(),b'previous')
        self.assertEqual(Path(result['output']).name,'out (1).png')
        with Image.open(result['output']) as img:
            self.assertEqual(img.info['icc_profile'],b'test-profile')
            self.assertEqual(img.size,(513,257))
            np.testing.assert_array_equal(np.array(img),self.image)
    def test_complementary_focus_recovers_details(self):
        blur=cv2.GaussianBlur(self.image,(0,0),4)
        a=self.image.copy();a[:,256:]=blur[:,256:]
        b=self.image.copy();b[:,:256]=blur[:,:256]
        result=stack([self.save('a.png',a),self.save('b.png',b)],self.root/'out.png',align=False)
        with Image.open(result['output']) as img:actual=np.array(img).astype(float)
        mask=np.zeros(self.image.shape[:2],bool);mask[40:-40,40:190]=True;mask[40:-40,320:-40]=True
        error=np.mean((actual[mask]-self.image[mask])**2)
        original=min(np.mean((a[mask].astype(float)-self.image[mask])**2),np.mean((b[mask].astype(float)-self.image[mask])**2))
        self.assertLess(error,original*.15)
    def test_alignment_translation_rotation_scale(self):
        matrix=cv2.getRotationMatrix2D((256,128),1.2,1.01);matrix[:,2]+=[4,-3]
        moved=cv2.warpAffine(self.image,matrix,(513,257),borderMode=cv2.BORDER_REFLECT_101)
        aligned,valid,info=align_image(moved,self.image)
        expected=np.vstack([cv2.invertAffineTransform(matrix),[0,0,1]])
        self.assertLess(np.max(np.abs(np.array(info['matrix'])-expected[:2])),.7)
        self.assertGreater(valid.mean(),.9)
    def test_alignment_failure_does_not_save(self):
        a=self.save('a.png',np.zeros_like(self.image));b=self.save('b.png',np.full_like(self.image,100))
        with self.assertRaises(StackError):stack([a,b],self.root/'out.png')
        self.assertFalse((self.root/'out.png').exists())
    def test_cancel_before_start(self):
        event=threading.Event();event.set()
        with self.assertRaises(Cancelled):stack(['a','b'],self.root/'out.png',cancel=event)
        self.assertFalse((self.root/'out.png').exists())
    def test_cancel_during_processing(self):
        a=self.save('a.png',self.image);b=self.save('b.png',self.image)
        event=threading.Event()
        with self.assertRaises(Cancelled):stack([a,b],self.root/'out.png',cancel=event,progress=lambda p,m:event.set())
        self.assertFalse((self.root/'out.png').exists())
    def test_validation(self):
        a=self.save('a.png',self.image);b=self.save('b.png',self.image[:100])
        for paths,kwargs in [([a],{}),([a,a],{}),([a,b],{}),([a,b],{'memory_mb':1}),([a,b],{'window':4})]:
            with self.subTest(paths=paths,kwargs=kwargs):
                with self.assertRaises(StackError):stack(paths,self.root/'out.png',**kwargs)
    def test_mismatched_profile_rejected(self):
        a=self.save('a.png',self.image);b=self.save('b.png',self.image,icc_profile=b'other')
        with self.assertRaises(StackError):stack([a,b],self.root/'out.png')
    def test_non8bit_rejected(self):
        a=self.save('a.tif',np.ones((100,100),np.uint16)*40000)
        b=self.save('b.tif',np.ones((100,100),np.uint16)*40000)
        with self.assertRaises(StackError):stack([a,b],self.root/'out.png')


if __name__=='__main__':unittest.main(verbosity=2)
